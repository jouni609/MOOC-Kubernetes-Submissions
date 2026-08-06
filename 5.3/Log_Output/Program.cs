var randomString = Guid.NewGuid().ToString();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var greeterUrl = Environment.GetEnvironmentVariable("GREETER_URL") ?? "http://greeter-svc:8080/";

async Task<string> FetchGreetingAsync(IHttpClientFactory httpClientFactory)
{
    try
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        var response = await client.GetAsync(greeterUrl);
        if (response.IsSuccessStatusCode)
        {
            return (await response.Content.ReadAsStringAsync()).Trim();
        }

        Console.WriteLine($"Greeter returned {response.StatusCode}");
        return "greeting unavailable";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Greeter error: {ex.Message}");
        return "greeting unavailable";
    }
}

string FormatOutput(string greeting)
{
    var timestamp = DateTime.UtcNow.ToString("o");
    return $"{timestamp}: {randomString}\n{greeting}";
}

_ = Task.Run(async () =>
{
    var factory = app.Services.GetRequiredService<IHttpClientFactory>();
    while (true)
    {
        var greeting = await FetchGreetingAsync(factory);
        Console.WriteLine(FormatOutput(greeting));
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

app.MapGet("/", async (IHttpClientFactory httpClientFactory) =>
{
    var greeting = await FetchGreetingAsync(httpClientFactory);
    var body = FormatOutput(greeting);
    Console.WriteLine($"[GET /]\n{body}");
    return Results.Text(body, "text/plain");
});

Console.WriteLine($"Log_Output on :{port}, GREETER_URL={greeterUrl}");
app.Run();
