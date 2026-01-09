using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for SoapFieldEntity.
/// </summary>
public class SoapFieldConfiguration : IEntityTypeConfiguration<SoapFieldEntity>
{
    public void Configure(EntityTypeBuilder<SoapFieldEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FieldType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.XPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Source)
            .HasMaxLength(1000);

        builder.Property(e => e.Namespace)
            .HasMaxLength(500);

        builder.Property(e => e.Prefix)
            .HasMaxLength(50);

        builder.Property(e => e.Attributes)
            .HasColumnType("jsonb"); // PostgreSQL-specific JSON column type

        builder.Property(e => e.Order)
            .IsRequired();

        // Create index for ordering
        builder.HasIndex(e => new { e.SoapConfigId, e.Order });
    }
}
