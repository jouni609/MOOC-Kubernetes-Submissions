// Todo_Backend Application: Exercise 3.6
// PostgreSQL-backed TODO item management with request logging & 140-character limit enforcement.

using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
var maxTodoLength = int.TryParse(Environment.GetEnvironmentVariable("MAX_TODO_LENGTH"), out var m) ? m : 140;

var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres-svc";
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "postgres";
var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";

var connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseCors();

// Request logging middleware to log every incoming HTTP request to stdout
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;
    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    Console.WriteLine($"[INFO] [HTTP REQUEST] {method} {path} from {remoteIp}");
    await next();
});

NpgsqlDataSource GetDataSource()
{
    return NpgsqlDataSource.Create(connectionString);
}

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
                CREATE TABLE IF NOT EXISTS todos (
                    id VARCHAR(50) PRIMARY KEY,
                    text VARCHAR(1000) NOT NULL,
                    completed BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at VARCHAR(100) NOT NULL
                );
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

// GKE Ingress health checks default to GET / and require HTTP 200.
app.MapGet("/", () => Results.Text("ok"));
app.MapGet("/health", () => Results.Text("ok"));

app.MapGet("/todos", async () =>
{
    try
    {
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT id, text, completed, created_at FROM todos ORDER BY created_at ASC;";
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<TodoItem>();
        while (await reader.ReadAsync())
        {
            list.Add(new TodoItem(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetString(3)
            ));
        }

        Console.WriteLine($"[INFO] [FETCH_TODOS] GET /todos -> returning {list.Count} items from PostgreSQL");
        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] [FETCH_TODOS] Error during GET /todos: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapPost("/todos", async (HttpContext context) =>
{
    string text = "";
    if (context.Request.HasJsonContentType())
    {
        var request = await context.Request.ReadFromJsonAsync<CreateTodoRequest>();
        text = request?.Text ?? request?.Todo ?? "";
    }
    else if (context.Request.HasFormContentType)
    {
        var form = await context.Request.ReadFormAsync();
        text = form["todo"].ToString() ?? form["text"].ToString() ?? "";
    }

    text = text.Trim();

    // Log incoming TODO attempt
    Console.WriteLine($"[INFO] [TODO_ATTEMPT] Received TODO creation request - Text length: {text.Length} characters");

    if (string.IsNullOrWhiteSpace(text))
    {
        Console.WriteLine("[WARN] [REJECTED] TODO text is empty or whitespace.");
        return Results.BadRequest(new { error = "TODO text cannot be empty." });
    }

    if (text.Length > maxTodoLength)
    {
        var preview = text.Length > 80 ? text[..80] + "..." : text;
        var rejectedLog = $"[WARN] [REJECTED] TODO text exceeds maximum limit of {maxTodoLength} characters ({text.Length} chars): '{preview}'";
        Console.WriteLine(rejectedLog);
        return Results.BadRequest(new { error = $"TODO text exceeds maximum limit of {maxTodoLength} characters. Received {text.Length} characters." });
    }

    var newItem = new TodoItem(
        Guid.NewGuid().ToString(),
        text,
        false,
        DateTime.UtcNow.ToString("o")
    );

    try
    {
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = "INSERT INTO todos (id, text, completed, created_at) VALUES (@id, @text, @completed, @created_at);";
        cmd.Parameters.AddWithValue("id", newItem.Id);
        cmd.Parameters.AddWithValue("text", newItem.Text);
        cmd.Parameters.AddWithValue("completed", newItem.Completed);
        cmd.Parameters.AddWithValue("created_at", newItem.CreatedAt);

        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"[INFO] [ACCEPTED] TODO saved to PostgreSQL: '{text}' (Length: {text.Length} chars, Id: {newItem.Id})");

        if (context.Request.HasFormContentType)
        {
            return Results.Redirect("/");
        }

        return Results.Created($"/todos/{newItem.Id}", newItem);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] [SAVE_TODO] Error saving TODO to PostgreSQL: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

Console.WriteLine($"[INFO] Todo_Backend server started on port {port}. Connected to PostgreSQL at {host}:{dbPort}, MAX_TODO_LENGTH: {maxTodoLength}.");
app.Run();

public record TodoItem(string Id, string Text, bool Completed, string CreatedAt);
public record CreateTodoRequest(string? Text, string? Todo);
