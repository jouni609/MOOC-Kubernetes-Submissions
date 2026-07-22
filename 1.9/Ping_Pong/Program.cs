// Ping_Pong Application: Exercise 1.9
// Responds with "pong <counter>" to GET /pingpong and increments counter in memory.

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment or default to 3000
var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var counter = 0;

app.MapGet("/pingpong", () =>
{
    var message = $"pong {counter}";
    Console.WriteLine($"GET /pingpong -> {message}");
    counter++;
    return message;
});

Console.WriteLine($"Ping-pong server starting on port {port}...");
app.Run();
