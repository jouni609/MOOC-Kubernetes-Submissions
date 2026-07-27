// Todo_Backend Application: Exercise 2.6
// Persistent TODO item management saving and loading items from a JSON file (TODOS_FILE_PATH).

using System.Text.Json;

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
var todosFilePath = Environment.GetEnvironmentVariable("TODOS_FILE_PATH") ?? "/usr/src/app/files/todos.json";

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseCors();

void EnsureFileExists()
{
    var dir = Path.GetDirectoryName(todosFilePath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }

    if (!File.Exists(todosFilePath))
    {
        File.WriteAllText(todosFilePath, "[]");
    }
}

List<TodoItem> LoadTodos()
{
    try
    {
        EnsureFileExists();
        var json = File.ReadAllText(todosFilePath);
        return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? new List<TodoItem>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading todos from file ({todosFilePath}): {ex.Message}");
        return new List<TodoItem>();
    }
}

void SaveTodos(List<TodoItem> list)
{
    try
    {
        EnsureFileExists();
        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(todosFilePath, json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving todos to file ({todosFilePath}): {ex.Message}");
    }
}

// Ensure storage file exists on startup
EnsureFileExists();

// GET /todos - returns all TODO items loaded from file
app.MapGet("/todos", () =>
{
    var todos = LoadTodos();
    Console.WriteLine($"GET /todos -> returning {todos.Count} items from file ({todosFilePath})");
    return Results.Ok(todos);
});

// POST /todos - accepts JSON body or Form data to add a new TODO item
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

    var todos = LoadTodos();
    var newItem = new TodoItem(
        Guid.NewGuid().ToString(),
        text,
        false,
        DateTime.UtcNow.ToString("o")
    );

    todos.Add(newItem);
    SaveTodos(todos);
    Console.WriteLine($"POST /todos -> Saved TODO: '{text}' (Id: {newItem.Id}) to file.");

    if (context.Request.HasFormContentType)
    {
        return Results.Redirect("/");
    }

    return Results.Created($"/todos/{newItem.Id}", newItem);
});

Console.WriteLine($"Todo_Backend server started on port {port}. Storage file: {todosFilePath}, MAX_TODO_LENGTH: {maxTodoLength}.");
app.Run();

public record TodoItem(string Id, string Text, bool Completed, string CreatedAt);
public record CreateTodoRequest(string? Text, string? Todo);
