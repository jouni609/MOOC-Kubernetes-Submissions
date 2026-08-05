// Todo_Backend Application: Exercise 4.10 (code repo; config in dwk-config)
// PostgreSQL-backed TODO item management with /healthprobe DB readiness check.

using Npgsql;
using NATS.Net;

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

var connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};Timeout=3;";
var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://my-nats:4222";

// Reuse one NATS client for the process lifetime (connects lazily on first publish).
var natsClient = new NatsClient(natsUrl);

async Task PublishNatsMessageAsync(string message)
{
    try
    {
        await natsClient.PublishAsync("todo_events", message);
        Console.WriteLine($"[INFO] [NATS_PUBLISH] Published message to 'todo_events': {message}");
    }
    catch (Exception ex)
    {
        // Missing status messages are acceptable for this exercise; never fail the HTTP request.
        Console.WriteLine($"[ERROR] [NATS_PUBLISH] Failed to publish message to NATS: {ex.Message}");
    }
}

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

async Task EnsureDatabaseInitializedAsync()
{
    await using var dataSource = GetDataSource();
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd = conn.CreateCommand();

    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS todos (
            id VARCHAR(50) PRIMARY KEY,
            text VARCHAR(1000) NOT NULL,
            completed BOOLEAN NOT NULL DEFAULT FALSE,
            created_at VARCHAR(100) NOT NULL
        );
    ";
    await cmd.ExecuteNonQueryAsync();
}

// Liveness Probe endpoints
app.MapGet("/", () => Results.Text("ok"));
app.MapGet("/health", () => Results.Text("ok"));

// GET /healthprobe — Readiness Probe endpoint: tests DB connectivity
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
        return Results.Ok(new { status = "ok" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] [PROBE] Todo_Backend DB check failed: {ex.Message}");
        return Results.Problem($"Database connection failed: {ex.Message}", statusCode: 500);
    }
});

app.MapGet("/todos", async () =>
{
    try
    {
        await EnsureDatabaseInitializedAsync();
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

app.MapPut("/todos/{id}", async (string id, UpdateTodoRequest? request) =>
{
    if (request is null || (request.Done is null && request.Completed is null))
    {
        return Results.BadRequest(new { error = "Request must include a boolean done field." });
    }

    var completed = request.Done ?? request.Completed!.Value;

    try
    {
        await EnsureDatabaseInitializedAsync();
        await using var dataSource = GetDataSource();
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            UPDATE todos
            SET completed = @completed
            WHERE id = @id
            RETURNING id, text, completed, created_at;
        ";
        cmd.Parameters.AddWithValue("completed", completed);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return Results.NotFound(new { error = $"TODO with id '{id}' was not found." });
        }

        var updatedItem = new TodoItem(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetBoolean(2),
            reader.GetString(3)
        );

        Console.WriteLine($"[INFO] [UPDATE_TODO] TODO {id} marked as {(completed ? "done" : "pending")}");
        await PublishNatsMessageAsync("A todo was updated");
        return Results.Ok(updatedItem);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] [UPDATE_TODO] Error updating TODO {id}: {ex.Message}");
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
        await EnsureDatabaseInitializedAsync();
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
        await PublishNatsMessageAsync("A todo was created");

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

Console.WriteLine($"[INFO] Todo_Backend server started on port {port}. PostgreSQL: {host}:{dbPort}, NATS: {natsUrl}, MAX_TODO_LENGTH: {maxTodoLength}.");
app.Run();

public record TodoItem(string Id, string Text, bool Completed, string CreatedAt);
public record CreateTodoRequest(string? Text, string? Todo);
public record UpdateTodoRequest(bool? Done, bool? Completed);
