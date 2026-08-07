using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var configuredPort)
    ? configuredPort
    : 8080;

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres-svc";
var postgresPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var postgresDatabase = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
var connectionString =
    $"Host={postgresHost};Port={postgresPort};Database={postgresDatabase};" +
    $"Username={postgresUser};Password={postgresPassword};Timeout=3;";

await using var dataSource = NpgsqlDataSource.Create(connectionString);

async Task EnsureDatabaseInitializedAsync()
{
    await using var connection = await dataSource.OpenConnectionAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS pongs (
            id INT PRIMARY KEY,
            counter INT NOT NULL
        );
        INSERT INTO pongs (id, counter)
        VALUES (1, 0)
        ON CONFLICT (id) DO NOTHING;
        """;
    await command.ExecuteNonQueryAsync();
}

app.MapGet("/health", () => Results.Ok("OK"));

app.MapGet("/healthprobe", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        return Results.Ok("OK");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Database readiness check failed: {exception.Message}");
        return Results.Problem("Database is unavailable.", statusCode: 503);
    }
});

app.MapGet("/", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE pongs SET counter = counter + 1 WHERE id = 1 RETURNING counter;";
        var currentCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        var response = $"pong {currentCount - 1}";
        Console.WriteLine(response);
        return Results.Text(response, "text/plain");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Updating the counter failed: {exception.Message}");
        return Results.Problem("Unable to update the counter.", statusCode: 503);
    }
});

app.MapGet("/pongs", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT counter FROM pongs WHERE id = 1;";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        var response = $"Ping / Pongs: {count}";
        Console.WriteLine(response);
        return Results.Text(response, "text/plain");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Reading the counter failed: {exception.Message}");
        return Results.Problem("Unable to read the counter.", statusCode: 503);
    }
});

Console.WriteLine($"Ping-pong listening on port {port}.");
await app.RunAsync();
