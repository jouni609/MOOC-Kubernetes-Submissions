// Ping_Pong Application: Exercise 3.2
// PostgreSQL-backed counter. Exposed via GKE Ingress at /pingpong.
// GET / returns 200 so GKE Ingress / load balancer health checks succeed
// even though public traffic is mapped to /pingpong.

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

var connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};";

NpgsqlDataSource GetDataSource()
{
    return NpgsqlDataSource.Create(connectionString);
}

// Retry loop to ensure database connectivity on startup
void InitializeDatabase()
{
    var attempts = 0;
    const int maxAttempts = 30;

    while (attempts < maxAttempts)
    {
        attempts++;
        try
        {
            Console.WriteLine($"[DB Init] Attempt {attempts}/{maxAttempts} connecting to PostgreSQL at {host}:{dbPort}...");
            using var dataSource = GetDataSource();
            using var conn = dataSource.OpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS pongs (
                    id INT PRIMARY KEY,
                    counter INT NOT NULL
                );
                INSERT INTO pongs (id, counter) VALUES (1, 0) ON CONFLICT (id) DO NOTHING;
            ";
            cmd.ExecuteNonQuery();

            Console.WriteLine("[DB Init] PostgreSQL database initialized successfully.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB Init] Connection failed: {ex.Message}");
            if (attempts >= maxAttempts) throw;
            Thread.Sleep(2000);
        }
    }
}

InitializeDatabase();

// GET / — required by GKE Ingress health checks (default probe path is /)
app.MapGet("/", () =>
{
    Console.WriteLine("GET / -> ok (health)");
    return Results.Text("ok", "text/plain");
});

// GET /pingpong — increments counter in PostgreSQL and returns response (Ingress path)
app.MapGet("/pingpong", async () =>
{
    try
    {
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "UPDATE pongs SET counter = counter + 1 WHERE id = 1 RETURNING counter;";
        var result = await cmd.ExecuteScalarAsync();
        var currentCount = Convert.ToInt32(result);

        var message = $"pong {currentCount - 1}";
        Console.WriteLine($"GET /pingpong -> {message} (Next: {currentCount})");
        return Results.Text(message, "text/plain");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during GET /pingpong: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// GET /pongs — current counter for Log_Output (cluster-internal HTTP)
app.MapGet("/pongs", async () =>
{
    try
    {
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
