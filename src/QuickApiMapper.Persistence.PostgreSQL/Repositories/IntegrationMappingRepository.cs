using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Persistence.PostgreSQL.Repositories;

/// <summary>
/// PostgreSQL implementation of the integration mapping repository.
/// </summary>
public class IntegrationMappingRepository : IIntegrationMappingRepository
{
    private readonly QuickApiMapperDbContext _context;

    public IntegrationMappingRepository(QuickApiMapperDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<IntegrationMappingEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.IntegrationMappings
            .Where(im => im.IsActive)
            .Include(im => im.FieldMappings)
                .ThenInclude(fm => fm.Transformers)
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields)
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Manually order child collections by Order property
        foreach (var entity in entities)
        {
            OrderChildCollections(entity);
        }

        return entities;
    }

    public async Task<IntegrationMappingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IntegrationMappings
            .Include(im => im.FieldMappings)
                .ThenInclude(fm => fm.Transformers)
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields)
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Id == id, cancellationToken);

        if (entity != null)
        {
            OrderChildCollections(entity);
        }

        return entity;
    }

    public async Task<IntegrationMappingEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IntegrationMappings
            .Include(im => im.FieldMappings)
                .ThenInclude(fm => fm.Transformers)
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields)
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Name == name, cancellationToken);

        if (entity != null)
        {
            OrderChildCollections(entity);
        }

        return entity;
    }

    public async Task<IntegrationMappingEntity?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IntegrationMappings
            .Include(im => im.FieldMappings)
                .ThenInclude(fm => fm.Transformers)
            .Include(im => im.StaticValues)
            .Include(im => im.SoapConfig)
                .ThenInclude(sc => sc!.Fields)
            .Include(im => im.GrpcConfig)
            .Include(im => im.ServiceBusConfig)
            .Include(im => im.RabbitMqConfig)
            .AsNoTracking()
            .FirstOrDefaultAsync(im => im.Endpoint == endpoint, cancellationToken);

        if (entity != null)
        {
            OrderChildCollections(entity);
        }

        return entity;
    }

    public async Task<IntegrationMappingEntity> AddAsync(IntegrationMappingEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.IntegrationMappings.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(IntegrationMappingEntity entity, CancellationToken cancellationToken = default)
    {
        _context.IntegrationMappings.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IntegrationMappings.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.IntegrationMappings.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<StaticValueEntity>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.StaticValues
            .Where(sv => sv.IsGlobal)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Orders child collections by their Order property.
    /// </summary>
    private static void OrderChildCollections(IntegrationMappingEntity entity)
    {
        // Order FieldMappings by Order property and replace the collection
        if (entity.FieldMappings != null && entity.FieldMappings.Count > 0)
        {
            // Order each FieldMapping's Transformers first
            foreach (var fieldMapping in entity.FieldMappings)
            {
                if (fieldMapping.Transformers != null && fieldMapping.Transformers.Count > 0)
                {
                    fieldMapping.Transformers = fieldMapping.Transformers.OrderBy(t => t.Order).ToList();
                }
            }

            // Then order the FieldMappings themselves
            entity.FieldMappings = entity.FieldMappings.OrderBy(fm => fm.Order).ToList();
        }

        // Order SoapConfig.Fields if present
        if (entity.SoapConfig?.Fields != null && entity.SoapConfig.Fields.Count > 0)
        {
            entity.SoapConfig.Fields = entity.SoapConfig.Fields.OrderBy(f => f.Order).ToList();
        }
    }
}
