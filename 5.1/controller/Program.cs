// DummySite controller (Exercise 5.1)
// Polls DummySite CRs (k8s.exercise/v1) and ensures Deployment + Service exist per site.

using System.Text.Json;
using k8s;
using k8s.Autorest;
using k8s.Models;

const string Group = "k8s.exercise";
const string Version = "v1";
const string Plural = "dummysites";
const string SiteImage = "dummysite-app:5.1";

var config = KubernetesClientConfiguration.IsInCluster()
    ? KubernetesClientConfiguration.InClusterConfig()
    : KubernetesClientConfiguration.BuildConfigFromConfigFile();

var client = new Kubernetes(config);
Console.WriteLine("[INFO] DummySite controller started (poll loop)");

while (true)
{
    try
    {
        await ReconcileAllAsync(client);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Reconcile cycle failed: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(5));
}

static async Task ReconcileAllAsync(IKubernetes client)
{
    var list = await client.CustomObjects.ListClusterCustomObjectAsync(
        Group, Version, Plural);

    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(list));
    if (!doc.RootElement.TryGetProperty("items", out var items))
    {
        return;
    }

    foreach (var item in items.EnumerateArray())
    {
        await ReconcileAsync(client, item);
    }
}

static async Task ReconcileAsync(IKubernetes client, JsonElement item)
{
    try
    {
        var name = item.GetProperty("metadata").GetProperty("name").GetString()
            ?? throw new InvalidOperationException("DummySite missing metadata.name");
        var ns = item.GetProperty("metadata").TryGetProperty("namespace", out var nsEl)
            ? nsEl.GetString() ?? "default"
            : "default";
        var uid = item.GetProperty("metadata").GetProperty("uid").GetString()
            ?? throw new InvalidOperationException("DummySite missing metadata.uid");

        if (!item.TryGetProperty("spec", out var spec) ||
            !spec.TryGetProperty("website_url", out var urlEl))
        {
            Console.WriteLine($"[WARN] DummySite {ns}/{name} has no spec.website_url");
            return;
        }

        var websiteUrl = urlEl.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            Console.WriteLine($"[WARN] DummySite {ns}/{name} website_url is empty");
            return;
        }

        var resourceName = $"dummysite-{name}";
        var labels = new Dictionary<string, string>
        {
            ["app"] = "dummysite",
            ["dummysite"] = name,
        };

        var owner = new V1OwnerReference
        {
            ApiVersion = $"{Group}/{Version}",
            Kind = "DummySite",
            Name = name,
            Uid = uid,
            Controller = true,
            BlockOwnerDeletion = true,
        };

        var deployChanged = await EnsureDeploymentAsync(
            client, ns, resourceName, websiteUrl, labels, owner);
        var serviceCreated = await EnsureServiceAsync(
            client, ns, resourceName, labels, owner);

        if (deployChanged || serviceCreated)
        {
            Console.WriteLine(
                $"[INFO] Reconciled DummySite {ns}/{name} url={websiteUrl} -> {resourceName}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Reconcile failed: {ex.Message}");
    }
}

static async Task<bool> EnsureDeploymentAsync(
    IKubernetes client,
    string ns,
    string resourceName,
    string websiteUrl,
    Dictionary<string, string> labels,
    V1OwnerReference owner)
{
    var deployment = BuildDeployment(ns, resourceName, websiteUrl, labels, owner);

    try
    {
        var existing = await client.AppsV1.ReadNamespacedDeploymentAsync(resourceName, ns);
        var currentUrl = existing.Spec?.Template?.Spec?.Containers?
            .FirstOrDefault()?.Env?
            .FirstOrDefault(e => e.Name == "WEBSITE_URL")?.Value;

        if (currentUrl == websiteUrl)
        {
            return false;
        }

        deployment.Metadata.ResourceVersion = existing.Metadata.ResourceVersion;
        deployment.Spec.Template.Metadata.Annotations = new Dictionary<string, string>
        {
            ["dummysite/website_url"] = websiteUrl,
        };
        await client.AppsV1.ReplaceNamespacedDeploymentAsync(deployment, resourceName, ns);
        Console.WriteLine($"[INFO] Updated Deployment {ns}/{resourceName}");
        return true;
    }
    catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        await client.AppsV1.CreateNamespacedDeploymentAsync(deployment, ns);
        Console.WriteLine($"[INFO] Created Deployment {ns}/{resourceName}");
        return true;
    }
}

static async Task<bool> EnsureServiceAsync(
    IKubernetes client,
    string ns,
    string resourceName,
    Dictionary<string, string> labels,
    V1OwnerReference owner)
{
    var service = new V1Service
    {
        ApiVersion = "v1",
        Kind = "Service",
        Metadata = new V1ObjectMeta
        {
            Name = resourceName,
            NamespaceProperty = ns,
            Labels = labels,
            OwnerReferences = new List<V1OwnerReference> { owner },
        },
        Spec = new V1ServiceSpec
        {
            Selector = labels,
            Ports = new List<V1ServicePort>
            {
                new()
                {
                    Name = "http",
                    Port = 80,
                    TargetPort = 8080,
                },
            },
        },
    };

    try
    {
        await client.CoreV1.ReadNamespacedServiceAsync(resourceName, ns);
        return false;
    }
    catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        await client.CoreV1.CreateNamespacedServiceAsync(service, ns);
        Console.WriteLine($"[INFO] Created Service {ns}/{resourceName}");
        return true;
    }
}

static V1Deployment BuildDeployment(
    string ns,
    string resourceName,
    string websiteUrl,
    Dictionary<string, string> labels,
    V1OwnerReference owner)
{
    return new V1Deployment
    {
        ApiVersion = "apps/v1",
        Kind = "Deployment",
        Metadata = new V1ObjectMeta
        {
            Name = resourceName,
            NamespaceProperty = ns,
            Labels = labels,
            OwnerReferences = new List<V1OwnerReference> { owner },
        },
        Spec = new V1DeploymentSpec
        {
            Replicas = 1,
            Selector = new V1LabelSelector { MatchLabels = labels },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta
                {
                    Labels = labels,
                    Annotations = new Dictionary<string, string>
                    {
                        ["dummysite/website_url"] = websiteUrl,
                    },
                },
                Spec = new V1PodSpec
                {
                    Containers = new List<V1Container>
                    {
                        new()
                        {
                            Name = "site",
                            Image = SiteImage,
                            ImagePullPolicy = "IfNotPresent",
                            Env = new List<V1EnvVar>
                            {
                                new() { Name = "WEBSITE_URL", Value = websiteUrl },
                                new() { Name = "PORT", Value = "8080" },
                            },
                            Ports = new List<V1ContainerPort>
                            {
                                new() { ContainerPort = 8080, Name = "http" },
                            },
                            ReadinessProbe = new V1Probe
                            {
                                HttpGet = new V1HTTPGetAction
                                {
                                    Path = "/healthz",
                                    Port = 8080,
                                },
                                InitialDelaySeconds = 2,
                                PeriodSeconds = 5,
                            },
                        },
                    },
                },
            },
        },
    };
}
