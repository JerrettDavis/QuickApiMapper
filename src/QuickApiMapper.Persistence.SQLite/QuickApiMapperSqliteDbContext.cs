using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.SQLite;

/// <summary>
/// Entity Framework Core DbContext for QuickApiMapper using SQLite.
/// </summary>
public class QuickApiMapperSqliteDbContext : DbContext
{
    public QuickApiMapperSqliteDbContext(DbContextOptions<QuickApiMapperSqliteDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<IntegrationMappingEntity> IntegrationMappings => Set<IntegrationMappingEntity>();
    public DbSet<FieldMappingEntity> FieldMappings => Set<FieldMappingEntity>();
    public DbSet<TransformerConfigEntity> Transformers => Set<TransformerConfigEntity>();
    public DbSet<StaticValueEntity> StaticValues => Set<StaticValueEntity>();
    public DbSet<SoapConfigEntity> SoapConfigurations => Set<SoapConfigEntity>();
    public DbSet<SoapFieldEntity> SoapFields => Set<SoapFieldEntity>();
    public DbSet<GrpcConfigEntity> GrpcConfigurations => Set<GrpcConfigEntity>();
    public DbSet<ServiceBusConfigEntity> ServiceBusConfigurations => Set<ServiceBusConfigEntity>();
    public DbSet<RabbitMqConfigEntity> RabbitMqConfigurations => Set<RabbitMqConfigEntity>();
    public DbSet<GlobalToggleEntity> GlobalToggles => Set<GlobalToggleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuickApiMapperSqliteDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        foreach (var entry in ChangeTracker.Entries<IntegrationMappingEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.Version++;
            }
        }

        foreach (var entry in ChangeTracker.Entries<GlobalToggleEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
