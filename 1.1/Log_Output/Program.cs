// Log_Output: generates a random string on startup and prints it every 5 seconds with a timestamp.
// Ex 1.1

var randomString = Guid.NewGuid().ToString();

while (true)
{
    var timestamp = DateTime.UtcNow.ToString("o");
    Console.WriteLine($"{timestamp}: {randomString}");
    await Task.Delay(TimeSpan.FromSeconds(5));
}
