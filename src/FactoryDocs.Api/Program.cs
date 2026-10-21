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
    await EnsureDocumentVersionsTableAsync(db);

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
     await BackfillInitialVersionsAsync(db);
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

    db.DocumentVersions.Add(new DocumentVersion
    {
        DocumentId = document.Id,
        VersionNumber = "1.0",
        Title = document.Title,
        Category = document.Category,
        Description = document.Description,
        CreatedBy = "System",
        ChangeReason = "Initial version",
        CreatedAtUtc = document.CreatedAtUtc
    });

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


static async Task EnsureDocumentVersionsTableAsync(
    FactoryDocsDbContext db)
{
    if (db.Database.IsSqlite())
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS DocumentVersions (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                DocumentId INTEGER NOT NULL,
                VersionNumber TEXT NOT NULL,
                Title TEXT NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT NULL,
                CreatedBy TEXT NOT NULL,
                ChangeReason TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CONSTRAINT FK_DocumentVersions_Documents_DocumentId
                    FOREIGN KEY (DocumentId)
                    REFERENCES Documents (Id)
                    ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS
            IX_DocumentVersions_DocumentId_VersionNumber
            ON DocumentVersions (DocumentId, VersionNumber);
            """);
    }
    else if (db.Database.IsSqlServer())
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.DocumentVersions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.DocumentVersions (
                    Id int IDENTITY(1,1) NOT NULL
                        CONSTRAINT PK_DocumentVersions PRIMARY KEY,
                    DocumentId int NOT NULL,
                    VersionNumber nvarchar(30) NOT NULL,
                    Title nvarchar(200) NOT NULL,
                    Category nvarchar(100) NOT NULL,
                    Description nvarchar(2000) NULL,
                    CreatedBy nvarchar(150) NOT NULL,
                    ChangeReason nvarchar(1000) NOT NULL,
                    CreatedAtUtc datetime2 NOT NULL,
                    CONSTRAINT FK_DocumentVersions_Documents_DocumentId
                        FOREIGN KEY (DocumentId)
                        REFERENCES dbo.Documents (Id)
                        ON DELETE CASCADE
                );
            END;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name =
                    N'IX_DocumentVersions_DocumentId_VersionNumber'
                  AND object_id =
                    OBJECT_ID(N'dbo.DocumentVersions')
            )
            BEGIN
                CREATE UNIQUE INDEX
                    IX_DocumentVersions_DocumentId_VersionNumber
                ON dbo.DocumentVersions (DocumentId, VersionNumber);
            END;
            """);
    }
    else
    {
        throw new InvalidOperationException(
            "The configured database provider is not supported.");
    }
}

static async Task BackfillInitialVersionsAsync(
    FactoryDocsDbContext db)
{
    var documentsWithoutVersions = await db.Documents
        .Where(d => !db.DocumentVersions
            .Any(v => v.DocumentId == d.Id))
        .ToListAsync();

    foreach (var document in documentsWithoutVersions)
    {
        db.DocumentVersions.Add(new FactoryDocs.Api.Models.DocumentVersion
        {
            DocumentId = document.Id,
            VersionNumber = "1.0",
            Title = document.Title,
            Category = document.Category,
            Description = document.Description,
            CreatedBy = "System",
            ChangeReason = "Initial version migrated from existing document",
            CreatedAtUtc = document.CreatedAtUtc
        });
    }

    if (documentsWithoutVersions.Count > 0)
    {
        await db.SaveChangesAsync();
    }
}


app.MapGet(
    "/api/documents/{id:int}/versions",
    async (int id, FactoryDocsDbContext db) =>
{
    var documentExists = await db.Documents
        .AnyAsync(d => d.Id == id);

    if (!documentExists)
        return Results.NotFound();

    var versions = await db.DocumentVersions
        .AsNoTracking()
        .Where(v => v.DocumentId == id)
        .OrderByDescending(v => v.CreatedAtUtc)
        .ThenByDescending(v => v.Id)
        .ToListAsync();

    return Results.Ok(versions);
});

app.MapPost(
    "/api/documents/{id:int}/versions",
    async (
        int id,
        CreateDocumentVersionRequest request,
        FactoryDocsDbContext db) =>
{
    var document = await db.Documents
        .FirstOrDefaultAsync(d => d.Id == id);

    if (document is null)
        return Results.NotFound();

    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.VersionNumber))
        errors["versionNumber"] = ["Version number is required."];

    if (string.IsNullOrWhiteSpace(request.Title))
        errors["title"] = ["Title is required."];

    if (string.IsNullOrWhiteSpace(request.Category))
        errors["category"] = ["Category is required."];

    if (string.IsNullOrWhiteSpace(request.CreatedBy))
        errors["createdBy"] = ["Author is required."];

    if (string.IsNullOrWhiteSpace(request.ChangeReason))
        errors["changeReason"] = ["Change reason is required."];

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var versionNumber = request.VersionNumber.Trim();

    if (versionNumber.Length > 30)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["versionNumber"] =
                    ["Version number cannot exceed 30 characters."]
            });
    }

    if (request.Title.Trim().Length > 200 ||
        request.Category.Trim().Length > 100 ||
        (request.Description?.Length ?? 0) > 2000 ||
        request.CreatedBy.Trim().Length > 150 ||
        request.ChangeReason.Trim().Length > 1000)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["request"] = ["One or more fields exceed their maximum length."]
            });
    }

    var duplicate = await db.DocumentVersions.AnyAsync(v =>
        v.DocumentId == id &&
        v.VersionNumber == versionNumber);

    if (duplicate)
    {
        return Results.Conflict(new
        {
            message = "This version number already exists for the document."
        });
    }

    await using var transaction =
        await db.Database.BeginTransactionAsync();

    var version = new DocumentVersion
    {
        DocumentId = document.Id,
        VersionNumber = versionNumber,
        Title = request.Title.Trim(),
        Category = request.Category.Trim(),
        Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim(),
        CreatedBy = request.CreatedBy.Trim(),
        ChangeReason = request.ChangeReason.Trim(),
        CreatedAtUtc = DateTime.UtcNow
    };

    // Atualizar o documento atual sem alterar o seu estado.
    document.Title = version.Title;
    document.Category = version.Category;
    document.Description = version.Description;

    db.DocumentVersions.Add(version);

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Created(
        $"/api/documents/{id}/versions",
        version);
});

app.Run();

public sealed record CreateDocumentRequest(
    string DocumentNumber,
    string Title,
    string Category,
    string? Description,
    string? Status);

public sealed record CreateDocumentVersionRequest(
    string VersionNumber,
    string Title,
    string Category,
    string? Description,
    string CreatedBy,
    string ChangeReason);

public sealed record UpdateStatusRequest(string? Status);
