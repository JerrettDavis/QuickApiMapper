using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for StaticValueEntity.
/// </summary>
public class StaticValueConfiguration : IEntityTypeConfiguration<StaticValueEntity>
{
    public void Configure(EntityTypeBuilder<StaticValueEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Value)
            .IsRequired();

        // Create index for global values
        builder.HasIndex(e => e.IsGlobal);

        // Create composite index for key lookups
        builder.HasIndex(e => new { e.IntegrationMappingId, e.Key });
    }
}
