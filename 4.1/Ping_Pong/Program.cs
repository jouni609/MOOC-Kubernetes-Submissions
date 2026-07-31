// Ping_Pong Application: Exercise 4.1
// Pong counter stored in PostgreSQL database.
// Exposes GET /health for Liveness Probe, GET /healthprobe for Readiness Probe (tests DB connection).

using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres-svc";
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

var connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};Timeout=3;";

NpgsqlDataSource GetDataSource()
{
    return NpgsqlDataSource.Create(connectionString);
}

async Task EnsureDatabaseInitializedAsync()
{
    await using var dataSource = GetDataSource();
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();

    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS pongs (
            id INT PRIMARY KEY,
            counter INT NOT NULL
        );
        INSERT INTO pongs (id, counter) VALUES (1, 0) ON CONFLICT (id) DO NOTHING;
    ";
    await cmd.ExecuteNonQueryAsync();
}

// GET /health — Liveness Probe endpoint
app.MapGet("/health", () => Results.Ok("OK"));

// GET /healthprobe — Readiness Probe endpoint: tests database connectivity
app.MapGet("/healthprobe", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        await cmd.ExecuteScalarAsync();
        return Results.Ok("OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Readiness Probe] Ping_Pong DB check failed: {ex.Message}");
        return Results.Problem($"Database connection failed: {ex.Message}", statusCode: 500);
    }
});

// GET / — pong counter (public endpoint)
app.MapGet("/", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "UPDATE pongs SET counter = counter + 1 WHERE id = 1 RETURNING counter;";
        var result = await cmd.ExecuteScalarAsync();
        var currentCount = Convert.ToInt32(result);

        var message = $"pong {currentCount - 1}";
        Console.WriteLine($"GET / -> {message} (Next: {currentCount})");
        return Results.Text(message, "text/plain");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during GET /: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// GET /pongs — current counter for Log_Output (cluster-internal)
app.MapGet("/pongs", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT counter FROM pongs WHERE id = 1;";
        var result = await cmd.ExecuteScalarAsync();
        var count = Convert.ToInt32(result);

        var responseText = $"Ping / Pongs: {count}";
        Console.WriteLine($"GET /pongs -> {responseText}");
        return Results.Text(responseText, "text/plain");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during GET /pongs: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

Console.WriteLine($"Ping-pong server started on port {port}. Connected to PostgreSQL at {host}:{dbPort}.");
app.Run();
