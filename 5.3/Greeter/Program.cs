var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 8080;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var greeting = Environment.GetEnvironmentVariable("GREETING") ?? "hello";
var version = Environment.GetEnvironmentVariable("VERSION") ?? "v1";

app.MapGet("/", () =>
{
    Console.WriteLine($"[{version}] GET / -> {greeting}");
    return Results.Text(greeting, "text/plain");
});

Console.WriteLine($"Greeter {version} on :{port}, greeting={greeting}");
app.Run();
