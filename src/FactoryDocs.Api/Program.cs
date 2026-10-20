using FactoryDocs.Api.Data;
using FactoryDocs.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQLite is the default for an easy local setup on macOS, Windows and Linux.
// SQL Server remains supported by setting DatabaseProvider=SqlServer and providing
// ConnectionStrings__FactoryDocs through environment variables or user secrets.
var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("FactoryDocs");

builder.Services.AddDbContext<FactoryDocsDbContext>(options =>
{
    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteConnection = connectionString
            ?? $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "FactoryDocs.db")}";
        options.UseSqlite(sqliteConnection);
    }
    else if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DatabaseProvider is SqlServer, but ConnectionStrings:FactoryDocs is not configured.");
        }

        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException(
            $"Unsupported DatabaseProvider '{databaseProvider}'. Use 'Sqlite' or 'SqlServer'.");
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalFrontend");

// Convenience for the interview MVP. Use EF Core migrations in production.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FactoryDocsDbContext>();
    db.Database.EnsureCreated();

    if (!db.Documents.Any())
    {
        db.Documents.AddRange(
            new Document
            {
                DocumentNumber = "WI-MFG-001",
                Title = "Machine Start-up Work Instruction",
                Category = "Production",
                Description = "Fictional sample instruction for the demo.",
                Status = "Approved",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
            },
            new Document
            {
                DocumentNumber = "SOP-MNT-002",
                Title = "Preventive Maintenance Procedure",
                Category = "Maintenance",
                Description = "Fictional maintenance procedure for demonstration.",
                Status = "PendingApproval",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new Document
            {
                DocumentNumber = "QMS-001",
                Title = "Quality Inspection Checklist",
                Category = "Quality",
                Description = "Fictional quality checklist.",
                Status = "Draft",
                CreatedAtUtc = DateTime.UtcNow
            });
        db.SaveChanges();
    }
}

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "FactoryDocs.Api" }));

app.MapGet("/api/documents", async (string? search, FactoryDocsDbContext db) =>
{
    var query = db.Documents.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(d =>
            d.DocumentNumber.Contains(term) ||
            d.Title.Contains(term) ||
            d.Category.Contains(term) ||
            (d.Description != null && d.Description.Contains(term)));
    }

    var documents = await query.OrderByDescending(d => d.CreatedAtUtc).ToListAsync();
    return Results.Ok(documents);
});

app.MapGet("/api/documents/{id:int}", async (int id, FactoryDocsDbContext db) =>
{
    var document = await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
    return document is null ? Results.NotFound() : Results.Ok(document);
});

app.MapPost("/api/documents", async (CreateDocumentRequest request, FactoryDocsDbContext db) =>
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        errors["documentNumber"] = ["Document number is required."];
    if (string.IsNullOrWhiteSpace(request.Title))
        errors["title"] = ["Title is required."];
    if (string.IsNullOrWhiteSpace(request.Category))
        errors["category"] = ["Category is required."];

    var allowedStatuses = new[] { "Draft", "PendingApproval", "Approved", "Rejected", "Archived" };
    var status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim();

    if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        errors["status"] = ["Status must be Draft, PendingApproval, Approved, Rejected or Archived."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var documentNumber = request.DocumentNumber.Trim();
    var exists = await db.Documents.AnyAsync(d => d.DocumentNumber == documentNumber);
    if (exists)
        return Results.Conflict(new { message = "A document with this document number already exists." });

    var document = new Document
    {
        DocumentNumber = documentNumber,
        Title = request.Title.Trim(),
        Category = request.Category.Trim(),
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        Status = allowedStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase)),
        CreatedAtUtc = DateTime.UtcNow
    };

    db.Documents.Add(document);
    await db.SaveChangesAsync();

    return Results.Created($"/api/documents/{document.Id}", document);
});

app.MapPatch("/api/documents/{id:int}/status", async (int id, UpdateStatusRequest request, FactoryDocsDbContext db) =>
{
    var allowedStatuses = new[] { "Draft", "PendingApproval", "Approved", "Rejected", "Archived" };
    var status = request.Status?.Trim();

    if (string.IsNullOrWhiteSpace(status) ||
        !allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Status must be Draft, PendingApproval, Approved, Rejected or Archived."]
        });
    }

    var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id);
    if (document is null) return Results.NotFound();

    document.Status = allowedStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
    await db.SaveChangesAsync();

    return Results.Ok(document);
});

app.Run();

public sealed record CreateDocumentRequest(
    string DocumentNumber,
    string Title,
    string Category,
    string? Description,
    string? Status);

public sealed record UpdateStatusRequest(string? Status);
