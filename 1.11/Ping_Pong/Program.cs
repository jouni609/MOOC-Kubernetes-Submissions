// Ping_Pong Application: Exercise 1.11
// Saves the request count to a shared persistent volume file (/usr/src/app/files/pingpong.txt).

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var filePath = Environment.GetEnvironmentVariable("FILE_PATH") ?? "/usr/src/app/files/pingpong.txt";

var directory = Path.GetDirectoryName(filePath);
if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
}

// Read existing counter from file if present
var counter = 0;
if (File.Exists(filePath))
{
    if (int.TryParse(File.ReadAllText(filePath).Trim(), out var existingCount))
    {
        counter = existingCount;
        Console.WriteLine($"Initialized counter from file: {counter}");
    }
}

app.MapGet("/pingpong", () =>
{
    var currentCount = counter;
    var message = $"pong {currentCount}";
    counter++;

    try
    {
        File.WriteAllText(filePath, counter.ToString());
        Console.WriteLine($"GET /pingpong -> {message} (Persisted next count: {counter})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing to {filePath}: {ex.Message}");
    }

    return message;
});

Console.WriteLine($"Ping-pong server starting on port {port}. Storage path: {filePath}");
app.Run();
