// Log_Output_Writer: Exercise 1.10
// Generates a random string on startup and writes a timestamped line into a shared file every 5 seconds.

var randomString = Guid.NewGuid().ToString();
var filePath = Environment.GetEnvironmentVariable("FILE_PATH") ?? "/usr/src/app/files/log.txt";

var directory = Path.GetDirectoryName(filePath);
if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
{
    Directory.CreateDirectory(directory);
}

Console.WriteLine($"Log_Output_Writer started. Writing to {filePath} with randomString: {randomString}");

while (true)
{
    var timestamp = DateTime.UtcNow.ToString("o");
    var line = $"{timestamp}: {randomString}";

    try
    {
        File.WriteAllText(filePath, line);
        Console.WriteLine($"[Writer] Updated file: {line}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Writer Error] Failed to write to {filePath}: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(5));
}
