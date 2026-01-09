using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for TransformerConfigEntity.
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

        builder.Property(e => e.Arguments)
            .HasColumnType("jsonb"); // PostgreSQL-specific JSON column type

        // Create index for ordering
        builder.HasIndex(e => new { e.FieldMappingId, e.Order });
    }
}
