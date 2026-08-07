var randomString = Guid.NewGuid().ToString();
var filePath = Environment.GetEnvironmentVariable("FILE_PATH") ?? "/usr/src/app/files/log.txt";
var directory = Path.GetDirectoryName(filePath);

if (!string.IsNullOrEmpty(directory))
{
    Directory.CreateDirectory(directory);
}

while (true)
{
    var line = $"{DateTime.UtcNow:o}: {randomString}";

    try
    {
        await File.WriteAllTextAsync(filePath, line);
        Console.WriteLine(line);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Writing {filePath} failed: {exception.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(5));
}
