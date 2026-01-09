using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for SoapConfigEntity.
/// </summary>
public class SoapConfigConfiguration : IEntityTypeConfiguration<SoapConfigEntity>
{
    public void Configure(EntityTypeBuilder<SoapConfigEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.BodyWrapperFieldXPath)
            .HasMaxLength(500);

        // Configure relationship with SOAP fields
        builder.HasMany(e => e.Fields)
            .WithOne(f => f.SoapConfig)
            .HasForeignKey(f => f.SoapConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
