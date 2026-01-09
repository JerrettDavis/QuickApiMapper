using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.PostgreSQL.Configuration;

/// <summary>
/// Entity configuration for IntegrationMappingEntity.
/// </summary>
public class IntegrationMappingConfiguration : IEntityTypeConfiguration<IntegrationMappingEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.SourceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.DestinationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.DestinationUrl)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.DispatchFor)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(200);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(200);

        // Create unique index on endpoint
        builder.HasIndex(e => e.Endpoint)
            .IsUnique();

        // Create index on name for faster lookups
        builder.HasIndex(e => e.Name);

        // Create index on IsActive for filtered queries
        builder.HasIndex(e => e.IsActive);

        // Configure relationships
        builder.HasMany(e => e.FieldMappings)
            .WithOne(f => f.IntegrationMapping)
            .HasForeignKey(f => f.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.StaticValues)
            .WithOne(s => s.IntegrationMapping)
            .HasForeignKey(s => s.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.SoapConfig)
            .WithOne(s => s.IntegrationMapping)
            .HasForeignKey<SoapConfigEntity>(s => s.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.GrpcConfig)
            .WithOne(g => g.IntegrationMapping)
            .HasForeignKey<GrpcConfigEntity>(g => g.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ServiceBusConfig)
            .WithOne(sb => sb.IntegrationMapping)
            .HasForeignKey<ServiceBusConfigEntity>(sb => sb.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RabbitMqConfig)
            .WithOne(rmq => rmq.IntegrationMapping)
            .HasForeignKey<RabbitMqConfigEntity>(rmq => rmq.IntegrationMappingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
