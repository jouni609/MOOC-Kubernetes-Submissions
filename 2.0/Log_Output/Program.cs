// Log_Output Application: Exercise 2.0
// Generates a random string on startup, prints timestamped logs to stdout every 5 seconds,
// and exposes GET / endpoint that queries the Ping_Pong service over HTTP for current pong count.

var randomString = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var pingpongUrl = Environment.GetEnvironmentVariable("PINGPONG_URL") ?? "http://pingpong-svc:2345/pongs";

// Background task for stdout logging
_ = Task.Run(async () =>
{
    while (true)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        Console.WriteLine($"{timestamp}: {randomString}");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

// GET / endpoint fetches pong count from Ping_Pong service over HTTP
app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var currentTimestamp = DateTime.UtcNow.ToString("o");
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

    var result = $"{currentTimestamp}: {randomString}\n{pongsResponse}";
    Console.WriteLine($"[GET /] Returned: {result.Replace("\n", " | ")}");
    return Results.Text(result, "text/plain");
});

Console.WriteLine($"Log_Output server started on port {port}. Target PINGPONG_URL: {pingpongUrl}");
app.Run();
