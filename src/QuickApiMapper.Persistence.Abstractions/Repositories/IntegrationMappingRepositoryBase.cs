using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.Abstractions.Repositories;

/// <summary>
/// Base implementation of the integration mapping repository.
/// Provides common logic for all database providers.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public abstract class IntegrationMappingRepositoryBase<TContext> : IIntegrationMappingRepository
    where TContext : DbContext
{
    /// <summary>
    /// Gets the database context.
    /// </summary>
    protected readonly TContext Context;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationMappingRepositoryBase{TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    protected IntegrationMappingRepositoryBase(TContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets the DbSet for integration mappings from the context.
    /// </summary>
    protected abstract DbSet<IntegrationMappingEntity> IntegrationMappings { get; }

    /// <summary>
    /// Gets the DbSet for static values from the context.
    /// </summary>
    protected abstract DbSet<StaticValueEntity> StaticValues { get; }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<IntegrationMappingEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await IntegrationMappings
            .Where(im => im.IsActive)
            .Include(im => im.FieldMappings.OrderBy(fm => fm.Order))
                .ThenInclude(fm => fm.Transformers.OrderBy(t => t.Order))
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields.OrderBy(f => f.Order))
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IntegrationMappingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await IntegrationMappings
            .Include(im => im.FieldMappings.OrderBy(fm => fm.Order))
                .ThenInclude(fm => fm.Transformers.OrderBy(t => t.Order))
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields.OrderBy(f => f.Order))
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IntegrationMappingEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await IntegrationMappings
            .Include(im => im.FieldMappings.OrderBy(fm => fm.Order))
                .ThenInclude(fm => fm.Transformers.OrderBy(t => t.Order))
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields.OrderBy(f => f.Order))
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IntegrationMappingEntity?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await IntegrationMappings
            .Include(im => im.FieldMappings.OrderBy(fm => fm.Order))
                .ThenInclude(fm => fm.Transformers.OrderBy(t => t.Order))
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields.OrderBy(f => f.Order))
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Endpoint == endpoint, cancellationToken);
    }

    /// <inheritdoc />
    public virtual IntegrationMappingEntity Add(IntegrationMappingEntity entity)
    {
        IntegrationMappings.Add(entity);
        return entity;
    }

    /// <inheritdoc />
    public virtual void Update(IntegrationMappingEntity entity)
    {
        IntegrationMappings.Update(entity);
    }

    /// <inheritdoc />
    public virtual void Delete(Guid id)
    {
        var entity = IntegrationMappings.Find(id);
        if (entity != null)
        {
            IntegrationMappings.Remove(entity);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<StaticValueEntity>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default)
    {
        return await StaticValues
            .Where(sv => sv.IsGlobal)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
