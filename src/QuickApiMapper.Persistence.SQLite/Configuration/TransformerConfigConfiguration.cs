using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.SQLite.Configuration;

/// <summary>
/// Entity configuration for TransformerConfigEntity (SQLite-specific).
/// </summary>
public class TransformerConfigConfiguration : IEntityTypeConfiguration<TransformerConfigEntity>
{
    public void Configure(EntityTypeBuilder<TransformerConfigEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Order)
            .IsRequired();

        // SQLite uses TEXT for JSON (no native JSON type)
        builder.Property(e => e.Arguments)
            .HasColumnType("TEXT");

        // Create index for ordering
        builder.HasIndex(e => new { e.FieldMappingId, e.Order });
    }
}
