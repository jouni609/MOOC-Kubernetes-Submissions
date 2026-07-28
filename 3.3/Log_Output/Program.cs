// Log_Output Application: Exercise 3.3
// Generates a random string on startup, prints timestamped logs to stdout every 5 seconds,
// and exposes GET / that queries the Ping_Pong service over HTTP for the current pong count.
// Deployed to GKE and exposed via Gateway API (HTTPRoute) at path /.

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

// GET / — Gateway root path; also used by GKE load balancer health checks
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
