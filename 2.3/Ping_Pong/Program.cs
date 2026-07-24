// Ping_Pong Application: Exercise 2.3
// Keeps request count in memory and provides HTTP GET endpoints for counter manipulation and reading.

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var counter = 0;

// GET /pingpong - increments counter and returns response
app.MapGet("/pingpong", () =>
{
    var currentCount = counter;
    var message = $"pong {currentCount}";
    counter++;

    Console.WriteLine($"GET /pingpong -> {message}");
    return message;
});

// GET /pongs - returns current count of pongs for other services (e.g., Log Output app)
app.MapGet("/pongs", () =>
{
    var responseText = $"Ping / Pongs: {counter}";
    Console.WriteLine($"GET /pongs -> {responseText}");
    return responseText;
});

Console.WriteLine($"Ping-pong server started on port {port}.");
app.Run();
