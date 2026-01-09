using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for FieldMappingEntity.
/// </summary>
public class FieldMappingConfiguration : IEntityTypeConfiguration<FieldMappingEntity>
{
    public void Configure(EntityTypeBuilder<FieldMappingEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Source)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Destination)
            .HasMaxLength(1000);

        builder.Property(e => e.Order)
            .IsRequired();

        // Create index for ordering
        builder.HasIndex(e => new { e.IntegrationMappingId, e.Order });

        // Configure relationship with transformers
        builder.HasMany(e => e.Transformers)
            .WithOne(t => t.FieldMapping)
            .HasForeignKey(t => t.FieldMappingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
