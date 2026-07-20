// Server_Port: Starts a web server that outputs the port number it is running on when its started.
// Ex 1.2

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
var app = builder.Build();
Console.WriteLine($"Server started in port {port}");
app.Run();