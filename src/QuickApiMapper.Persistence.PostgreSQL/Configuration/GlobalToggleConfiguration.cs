using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for GlobalToggleEntity (PostgreSQL-specific).
/// </summary>
public class GlobalToggleConfiguration : IEntityTypeConfiguration<GlobalToggleEntity>
{
    public void Configure(EntityTypeBuilder<GlobalToggleEntity> builder)
    {
        builder.ToTable("global_toggles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("key");

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(e => e.IsEnabled)
            .IsRequired()
            .HasColumnName("is_enabled");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(200)
            .HasColumnName("updated_by");

        // Unique constraint on Key
        builder.HasIndex(e => e.Key)
            .IsUnique()
            .HasDatabaseName("ix_global_toggles_key");
    }
}
