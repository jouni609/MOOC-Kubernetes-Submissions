// Todo_Backend Application: Exercise 2.4
// In-memory TODO item management providing GET /todos and POST /todos endpoints.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 3000;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();
app.UseCors();

var todos = new List<TodoItem>
{
    new("1", "Learn Kubernetes basics", false, DateTime.UtcNow.AddHours(-2).ToString("o")),
    new("2", "Build Todo App & Backend", false, DateTime.UtcNow.AddHours(-1).ToString("o"))
};

// GET /todos - returns all TODO items
app.MapGet("/todos", () =>
{
    Console.WriteLine($"GET /todos -> returning {todos.Count} items");
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

    if (text.Length > 140)
    {
        return Results.BadRequest(new { error = "TODO text exceeds 140 characters." });
    }

    var newItem = new TodoItem(
        Guid.NewGuid().ToString(),
        text,
        false,
        DateTime.UtcNow.ToString("o")
    );

    todos.Add(newItem);
    Console.WriteLine($"POST /todos -> Created TODO: '{text}' (Id: {newItem.Id})");

    if (context.Request.HasFormContentType)
    {
        return Results.Redirect("/");
    }

    return Results.Created($"/todos/{newItem.Id}", newItem);
});

Console.WriteLine($"Todo_Backend server started on port {port}.");
app.Run();

public record TodoItem(string Id, string Text, bool Completed, string CreatedAt);
public record CreateTodoRequest(string? Text, string? Todo);
