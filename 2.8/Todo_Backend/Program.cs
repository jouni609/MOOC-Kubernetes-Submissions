// Todo_Backend Application: Exercise 2.8
// PostgreSQL-backed TODO item management using StatefulSet database persistence.

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
                CREATE TABLE IF NOT EXISTS todos (
                    id VARCHAR(50) PRIMARY KEY,
                    text VARCHAR(500) NOT NULL,
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

// GET /todos - returns all TODO items loaded from PostgreSQL
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

        Console.WriteLine($"GET /todos -> returning {list.Count} items from PostgreSQL");
        return Results.Ok(list);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during GET /todos: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// POST /todos - accepts JSON body or Form data to add a new TODO item to PostgreSQL
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

    if (string.IsNullOrWhiteSpace(text))
    {
        return Results.BadRequest(new { error = "TODO text cannot be empty." });
    }

    if (text.Length > maxTodoLength)
    {
        return Results.BadRequest(new { error = $"TODO text exceeds {maxTodoLength} characters." });
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
        Console.WriteLine($"POST /todos -> Saved TODO: '{text}' (Id: {newItem.Id}) to PostgreSQL.");

        if (context.Request.HasFormContentType)
        {
            return Results.Redirect("/");
        }

        return Results.Created($"/todos/{newItem.Id}", newItem);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving TODO to PostgreSQL: {ex.Message}");
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

Console.WriteLine($"Todo_Backend server started on port {port}. Connected to PostgreSQL at {host}:{dbPort}, MAX_TODO_LENGTH: {maxTodoLength}.");
app.Run();

public record TodoItem(string Id, string Text, bool Completed, string CreatedAt);
public record CreateTodoRequest(string? Text, string? Todo);
