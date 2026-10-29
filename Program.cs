using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. On enregistre la base de données en mémoire
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TaskPulseDb"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Route de test simple
app.MapGet("/", () => "API TaskPulse opérationnelle !");

app.MapGet("/tasks", async (AppDbContext db) =>
{
    var tasks = await db.Tasks.ToListAsync();
    return Results.Ok(tasks);
});

app.MapGet("/tasks/{id}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task == null)
    {
        return Results.NotFound();
    }
    else
    {
        return Results.Ok(task);
    }
});

app.MapPost("/tasks", async (TaskItem newTask, AppDbContext db) =>
{

    // Une tache ne peut pas avoir un titre null. Si le titre est null ou vide, on renvoie une erreur 400 Bad Request
    if (string.IsNullOrWhiteSpace(newTask.Title))
    {
        return Results.BadRequest("Le titre de la tâche ne peut pas être vide.");
    }

    // Validation des valeurs autorisées pour la priorité
    var prioritesValides = new[] { "Basse", "Moyenne", "Haute" };
    if (!prioritesValides.Contains(newTask.Priority))
    {
        return Results.BadRequest("La priorité doit être 'Basse', 'Moyenne' ou 'Haute'.");
    }

    db.Tasks.Add(newTask);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{newTask.Id}", newTask);    
});

app.MapPut("/tasks/{id}", async (int id, TaskItem updatedTask, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task == null)
    {
        return Results.NotFound();
    }

    // Validation du titre
    if (string.IsNullOrWhiteSpace(updatedTask.Title))
    {
        return Results.BadRequest("Le titre de la tâche ne peut pas être vide.");
    }

    // Validation des valeurs autorisées pour la priorité
    var prioritesValides = new[] { "Basse", "Moyenne", "Haute" };
    if (!prioritesValides.Contains(updatedTask.Priority))
    {
        return Results.BadRequest("La priorité doit être 'Basse', 'Moyenne' ou 'Haute'.");
    }

    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.IsCompleted = updatedTask.IsCompleted;
    task.Priority = updatedTask.Priority;

    await db.SaveChangesAsync();
    return Results.Ok(task);
});

app.MapDelete("/tasks/{id}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task == null)
    {
        return Results.NotFound();
    }

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// --------------------------------------------------------------------------------
// Methode GET 
// --------------------------------------------------------------------------------

app.MapGet("/tasks/completed", async ( AppDbContext db ) =>
{

    var completed = await db.Tasks.Where(t => t.IsCompleted == true).ToListAsync();
    
    return Results.Ok(completed);
});

app.MapGet("/tasks/priority/{level}", async (string level, AppDbContext db) =>
{
    var filtred = await db.Tasks.Where(t => t.Priority == level).ToListAsync();
    return Results.Ok(filtred);
});

app.Run();

// --- 2. LE MODÈLE (La table des tâches) ---
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public string Priority { get; set; } = "Moyenne"; // Basse, Moyenne, Haute
}

// --- 3. LE CONTEXTE DE BASE DE DONNÉES ---
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Représente la table "Tasks" dans notre base
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}