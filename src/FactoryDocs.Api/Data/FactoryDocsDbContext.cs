using FactoryDocs.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FactoryDocs.Api.Data;

public sealed class FactoryDocsDbContext(DbContextOptions<FactoryDocsDbContext> options)
    : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

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
    }
}
