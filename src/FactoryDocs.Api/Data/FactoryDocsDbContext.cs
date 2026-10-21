using FactoryDocs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FactoryDocs.Api.Data;

public sealed class FactoryDocsDbContext(DbContextOptions<FactoryDocsDbContext> options)
    : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentVersion> DocumentVersions =>
        Set<DocumentVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<Document>();
        document.HasKey(d => d.Id);
        document.HasIndex(d => d.DocumentNumber).IsUnique();
        document.Property(d => d.DocumentNumber).HasMaxLength(60).IsRequired();
        document.Property(d => d.Title).HasMaxLength(200).IsRequired();
        document.Property(d => d.Category).HasMaxLength(100).IsRequired();
        document.Property(d => d.Description).HasMaxLength(2000);
        document.Property(d => d.Status).HasMaxLength(30).IsRequired();

        var version = modelBuilder.Entity<DocumentVersion>();

        version.HasKey(v => v.Id);

        version.HasIndex(v => new
        {
            v.DocumentId,
            v.VersionNumber
        }).IsUnique();

        version.Property(v => v.VersionNumber)
            .HasMaxLength(30).IsRequired();

        version.Property(v => v.Title)
            .HasMaxLength(200).IsRequired();

        version.Property(v => v.Category)
            .HasMaxLength(100).IsRequired();

        version.Property(v => v.Description)
            .HasMaxLength(2000);

        version.Property(v => v.CreatedBy)
            .HasMaxLength(150).IsRequired();

        version.Property(v => v.ChangeReason)
            .HasMaxLength(1000).IsRequired();

        version.HasOne(v => v.Document)
            .WithMany()
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
