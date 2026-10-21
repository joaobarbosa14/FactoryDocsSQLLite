
using FactoryDocs.Api.Data;
using FactoryDocs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FactoryDocs.Api.Endpoints;

public static class DocumentVersionEndpoints
{
    public static IEndpointRouteBuilder MapDocumentVersionEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/documents/{id:int}/versions/{versionId:int}/restore",
            async (
                int id,
                int versionId,
                RestoreVersionRequest request,
                FactoryDocsDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(request.CreatedBy))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["createdBy"] = ["Author is required."]
                        });
                }

                if (string.IsNullOrWhiteSpace(request.ChangeReason))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["changeReason"] =
                                ["Reason for restoration is required."]
                        });
                }

                if (request.CreatedBy.Trim().Length > 150 ||
                    request.ChangeReason.Trim().Length > 1000)
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["request"] =
                                ["Author or reason exceeds the maximum length."]
                        });
                }

                var document = await db.Documents
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document is null)
                    return Results.NotFound(new
                    {
                        message = "Document not found."
                    });

                var sourceVersion = await db.DocumentVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v =>
                        v.Id == versionId &&
                        v.DocumentId == id);

                if (sourceVersion is null)
                    return Results.NotFound(new
                    {
                        message = "The selected version was not found."
                    });

                var latestVersion = await db.DocumentVersions
                    .Where(v => v.DocumentId == id)
                    .OrderByDescending(v => v.CreatedAtUtc)
                    .ThenByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

                var newVersionNumber = NextMinorVersion(
                    latestVersion?.VersionNumber ?? "1.0");

                await using var transaction =
                    await db.Database.BeginTransactionAsync();

                // Restaurar apenas os campos guardados no histórico.
                document.Title = sourceVersion.Title;
                document.Category = sourceVersion.Category;
                document.Description = sourceVersion.Description;

                var restoredVersion = new DocumentVersion
                {
                    DocumentId = document.Id,
                    VersionNumber = newVersionNumber,
                    Title = sourceVersion.Title,
                    Category = sourceVersion.Category,
                    Description = sourceVersion.Description,
                    CreatedBy = request.CreatedBy.Trim(),
                    ChangeReason =
                        $"Restored from version {sourceVersion.VersionNumber}: " +
                        request.ChangeReason.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                db.DocumentVersions.Add(restoredVersion);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Results.Created(
                    $"/api/documents/{id}/versions",
                    restoredVersion);
            });

        return app;
    }

    private static string NextMinorVersion(string currentVersion)
    {
        var parts = currentVersion.Split('.');

        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            major >= 0 &&
            minor >= 0 &&
            minor < int.MaxValue)
        {
            return $"{major}.{minor + 1}";
        }

        // Fallback para versões antigas com formato inesperado.
        return "1.1";
    }
}

public sealed record RestoreVersionRequest(
    string CreatedBy,
    string ChangeReason);