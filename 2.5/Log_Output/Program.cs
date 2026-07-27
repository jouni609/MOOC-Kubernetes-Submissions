// Log_Output Application: Exercise 2.5
// Demonstrates ConfigMap usage via environment variable (MESSAGE) and volume mount (information.txt).

var randomString = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var pingpongUrl = Environment.GetEnvironmentVariable("PINGPONG_URL") ?? "http://pingpong-svc:2345/pongs";

string GetConfigFilePath()
{
    var envPath = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH");
    if (!string.IsNullOrEmpty(envPath)) return envPath;
    if (File.Exists("/config/information.txt")) return "/config/information.txt";
    if (File.Exists("information.txt")) return "information.txt";
    return "/config/information.txt";
}

string GetFileContentLine()
{
    var configFilePath = GetConfigFilePath();
    if (!File.Exists(configFilePath))
    {
        return "file content: missing file";
    }

    var text = File.ReadAllText(configFilePath).Trim();
    return text.StartsWith("file content:", StringComparison.OrdinalIgnoreCase)
        ? text
        : $"file content: {text}";
}

string GetEnvVarLine()
{
    var message = Environment.GetEnvironmentVariable("MESSAGE") ?? "hello world";
    return message.StartsWith("env variable:", StringComparison.OrdinalIgnoreCase)
        ? message
        : $"env variable: MESSAGE={message}";
}

// Background task for stdout logging
_ = Task.Run(async () =>
{
    while (true)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        var fileLine = GetFileContentLine();
        var envLine = GetEnvVarLine();
        
        Console.WriteLine($"{fileLine}\n{envLine}\n{timestamp}: {randomString}");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

// GET / endpoint fetches pong count from Ping_Pong service over HTTP and includes ConfigMap info
app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var currentTimestamp = DateTime.UtcNow.ToString("o");
    var fileLine = GetFileContentLine();
    var envLine = GetEnvVarLine();
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

    var result = $"{fileLine}\n{envLine}\n{currentTimestamp}: {randomString}\n{pongsResponse}";
    Console.WriteLine($"[GET /] Returned:\n{result}");
    return Results.Text(result, "text/plain");
});

Console.WriteLine($"Log_Output server started on port {port}. Target PINGPONG_URL: {pingpongUrl}");
app.Run();
