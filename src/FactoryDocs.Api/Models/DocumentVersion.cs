
namespace FactoryDocs.Api.Models;

public sealed class DocumentVersion
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public required string VersionNumber { get; set; }

    public required string Title { get; set; }

    public required string Category { get; set; }

    public string? Description { get; set; }

    public required string CreatedBy { get; set; }

    public required string ChangeReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Document? Document { get; set; }
}