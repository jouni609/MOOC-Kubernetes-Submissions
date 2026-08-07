var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var configuredPort)
    ? configuredPort
    : 3000;

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "/usr/src/app/files/log.txt";
var pingpongUrl = Environment.GetEnvironmentVariable("PINGPONG_URL")
    ?? "http://pingpong.exercises.svc.cluster.local/pongs";

app.MapGet("/health", () => Results.Ok("OK"));

app.MapGet("/healthprobe", () => Results.Ok("OK"));

app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var logContent = File.Exists(logFilePath)
        ? await File.ReadAllTextAsync(logFilePath)
        : "Waiting for log content...";

    var pongsResponse = "Ping / Pongs: 0";

    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        var response = await client.GetAsync(pingpongUrl);

        if (response.IsSuccessStatusCode)
        {
            pongsResponse = (await response.Content.ReadAsStringAsync()).Trim();
        }
        else
        {
            Console.WriteLine($"Ping-pong returned {response.StatusCode}.");
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Fetching pongs failed: {exception.Message}");
    }

    var result = $"{logContent.Trim()}\n{pongsResponse}";
    Console.WriteLine(result.Replace("\n", " | "));
    return Results.Text(result, "text/plain");
});

Console.WriteLine($"Log Output Reader listening on port {port}.");
await app.RunAsync();
