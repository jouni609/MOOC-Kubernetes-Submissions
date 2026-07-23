// Log_Output_Reader: Exercise 1.11
// Reads both log.txt and pingpong.txt from the shared volume and outputs the combined status.

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "/usr/src/app/files/log.txt";
var pingpongFilePath = Environment.GetEnvironmentVariable("PINGPONG_FILE_PATH") ?? "/usr/src/app/files/pingpong.txt";

app.MapGet("/", () =>
{
    var logContent = File.Exists(logFilePath) ? File.ReadAllText(logFilePath).Trim() : "Waiting for log content...";
    var pingpongCount = File.Exists(pingpongFilePath) ? File.ReadAllText(pingpongFilePath).Trim() : "0";

    var responseText = $"{logContent}\nPing / Pongs: {pingpongCount}";
    Console.WriteLine($"[Reader] Served GET / -> {responseText.Replace("\n", " | ")}");
    
    return Results.Text(responseText, "text/plain");
});

Console.WriteLine($"Log_Output_Reader started on port {port}. Log file: {logFilePath}, Pingpong file: {pingpongFilePath}");
app.Run();
