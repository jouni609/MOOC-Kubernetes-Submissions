// Todo_Job Application: Exercise 2.9
// CronJob application that fetches a random Wikipedia article and posts a TODO ("Read <URL>") to Todo_Backend.

using System.Net;
using System.Text;
using System.Text.Json;

Console.WriteLine("[Job] Starting Wikipedia Random Article Todo Job...");

var wikiUrl = Environment.GetEnvironmentVariable("WIKI_URL") ?? "https://en.wikipedia.org/wiki/Special:Random";
var todoBackendUrl = Environment.GetEnvironmentVariable("TODO_BACKEND_URL") ?? "http://todo-backend-svc:2345/todos";

try
{
    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
    using var client = new HttpClient(handler);
    client.DefaultRequestHeaders.Add("User-Agent", "DevOpsWithKubernetes-TodoJob/1.0 (contact: student@mooc.fi)");

    Console.WriteLine($"[Job] Requesting random article from {wikiUrl}...");
    using var response = await client.GetAsync(wikiUrl);

    string targetArticleUrl = wikiUrl;

    if (response.StatusCode == HttpStatusCode.Redirect ||
        response.StatusCode == HttpStatusCode.MovedPermanently ||
        response.StatusCode == HttpStatusCode.Found ||
        response.StatusCode == HttpStatusCode.SeeOther ||
        response.StatusCode == HttpStatusCode.TemporaryRedirect)
    {
        var location = response.Headers.Location?.ToString();
        if (!string.IsNullOrEmpty(location))
        {
            if (location.StartsWith("http://") || location.StartsWith("https://"))
            {
                targetArticleUrl = location;
            }
            else if (location.StartsWith("//"))
            {
                targetArticleUrl = $"https:{location}";
            }
            else if (location.StartsWith("/"))
            {
                targetArticleUrl = $"https://en.wikipedia.org{location}";
            }
            else
            {
                targetArticleUrl = $"https://en.wikipedia.org/wiki/{location}";
            }
        }
    }
    else
    {
        targetArticleUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? wikiUrl;
    }

    var todoText = $"Read {targetArticleUrl}";
    Console.WriteLine($"[Job] Resolved Article URL: {targetArticleUrl}");
    Console.WriteLine($"[Job] Formatted TODO: '{todoText}'");

    using var postClient = new HttpClient();
    var payload = JsonSerializer.Serialize(new { text = todoText });
    var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

    Console.WriteLine($"[Job] Sending POST request to {todoBackendUrl}...");
    using var postResponse = await postClient.PostAsync(todoBackendUrl, httpContent);

    if (postResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("[Job] Successfully posted TODO to backend!");
    }
    else
    {
        var errBody = await postResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"[Job] Failed to post TODO. Status: {postResponse.StatusCode}, Body: {errBody}");
        Environment.Exit(1);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Job] Error executing job: {ex.Message}");
    Environment.Exit(1);
}
