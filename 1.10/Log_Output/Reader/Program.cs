// Log_Output_Reader: Exercise 1.10
// Reads the shared log file and provides its content via HTTP GET endpoint.

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var filePath = Environment.GetEnvironmentVariable("FILE_PATH") ?? "/usr/src/app/files/log.txt";

app.MapGet("/", () =>
{
    if (File.Exists(filePath))
    {
        var content = File.ReadAllText(filePath);
        Console.WriteLine($"[Reader] Served GET / -> {content}");
        return Results.Text(content, "text/plain");
    }
    
    Console.WriteLine($"[Reader] Shared file {filePath} not found yet.");
    return Results.Text("Waiting for log content...", "text/plain");
});

Console.WriteLine($"Log_Output_Reader started on port {port}. Reading from {filePath}");
app.Run();
