using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.SQLite.Configuration;

/// <summary>
/// Entity configuration for GlobalToggleEntity (SQLite-specific).
/// </summary>
public class GlobalToggleConfiguration : IEntityTypeConfiguration<GlobalToggleEntity>
{
    public void Configure(EntityTypeBuilder<GlobalToggleEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.IsEnabled)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(200);

        // Unique constraint on Key
        builder.HasIndex(e => e.Key)
            .IsUnique();
    }
}
