// Log_Output: Exercise 1.7
// Generates a random string on startup, logs timestamped status to stdout every 5s,
// and exposes a HTTP GET / endpoint returning the current status.

var randomString = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment or default to 3000
var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Background task for continuous stdout logging
_ = Task.Run(async () =>
{
    while (true)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        Console.WriteLine($"{timestamp}: {randomString}");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

// GET / endpoint returning current timestamp and stored random string
app.MapGet("/", () =>
{
    var currentTimestamp = DateTime.UtcNow.ToString("o");
    return $"{currentTimestamp}: {randomString}";
});

Console.WriteLine($"Server starting on port {port}...");
app.Run();
