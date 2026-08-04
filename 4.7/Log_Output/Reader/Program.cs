// Log_Output_Reader: Exercise 4.7
// Reads log.txt from shared volume, queries Ping_Pong service over HTTP for pong count,
// exposes GET /health for Liveness Probe, and GET /healthprobe for Readiness Probe.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "/usr/src/app/files/log.txt";
var pingpongUrl = Environment.GetEnvironmentVariable("PINGPONG_URL") ?? "http://pingpong-svc:2345/pongs";

// GET /health — Liveness Probe endpoint
app.MapGet("/health", () => Results.Ok("OK"));

// GET /healthprobe — Readiness Probe endpoint (checks Ping_Pong application availability)
app.MapGet("/healthprobe", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        var response = await client.GetAsync(pingpongUrl);
        if (response.IsSuccessStatusCode)
        {
            return Results.Ok("OK");
        }
        else
        {
            Console.WriteLine($"[Readiness Probe] Ping_Pong check failed with status: {response.StatusCode}");
            return Results.Problem($"Ping_Pong returned status {response.StatusCode}", statusCode: 500);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Readiness Probe] Ping_Pong check error: {ex.Message}");
        return Results.Problem($"Cannot connect to Ping_Pong: {ex.Message}", statusCode: 500);
    }
});

// GET / — Root endpoint
app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath).Trim() : "Waiting for log content...";
    var pongsResponse = "Ping / Pongs: 0";

    try
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(pingpongUrl);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            pongsResponse = content.StartsWith("Ping / Pongs:", StringComparison.OrdinalIgnoreCase)
                ? content
                : $"Ping / Pongs: {content}";
        }
        else
        {
            Console.WriteLine($"Failed to fetch pongs from {pingpongUrl}: {response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching pongs from {pingpongUrl}: {ex.Message}");
    }

    var result = $"{logContent}\n{pongsResponse}";
    Console.WriteLine($"[Reader] Served GET / -> {result.Replace("\n", " | ")}");
    return Results.Text(result, "text/plain");
});

Console.WriteLine($"Log_Output_Reader started on port {port}. Log file: {logFilePath}, Target PINGPONG_URL: {pingpongUrl}");
app.Run();
