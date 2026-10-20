namespace FactoryDocs.Api.Models;

public sealed class Document
{
    public int Id { get; set; }
    public required string DocumentNumber { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
