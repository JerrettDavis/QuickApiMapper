using System.Text.Json;
using QuickApiMapper.Management.Api.Models;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Management.Api.Services;

/// <summary>
/// Service for managing integration mappings.
/// </summary>
public class IntegrationService : IIntegrationService
{
    private readonly IIntegrationMappingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(
        IIntegrationMappingRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<IntegrationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<IntegrationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllActiveAsync(cancellationToken);
        return entities.Select(MapToDto);
    }

    public async Task<IntegrationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IntegrationDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByNameAsync(name, cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IntegrationDto?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEndpointAsync(endpoint, cancellationToken);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IntegrationDto> CreateAsync(CreateIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        // Check if integration with same name or endpoint already exists
        var existingByName = await _repository.GetByNameAsync(request.Name, cancellationToken);
        if (existingByName != null)
        {
            throw new InvalidOperationException($"Integration with name '{request.Name}' already exists");
        }

        var existingByEndpoint = await _repository.GetByEndpointAsync(request.Endpoint, cancellationToken);
        if (existingByEndpoint != null)
        {
            throw new InvalidOperationException($"Integration with endpoint '{request.Endpoint}' already exists");
        }

        var entity = MapToEntity(request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Version = 1;

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created integration {IntegrationId} with name {Name}", entity.Id, entity.Name);

        return MapToDto(entity);
    }

    public async Task<IntegrationDto> UpdateAsync(Guid id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Integration with ID {id} not found");
        }

        // Check if updating endpoint conflicts with another integration
        if (entity.Endpoint != request.Endpoint)
        {
            var existingByEndpoint = await _repository.GetByEndpointAsync(request.Endpoint, cancellationToken);
            if (existingByEndpoint != null && existingByEndpoint.Id != id)
            {
                throw new InvalidOperationException($"Integration with endpoint '{request.Endpoint}' already exists");
            }
        }

        // Update entity
        entity.Name = request.Name;
        entity.Endpoint = request.Endpoint;
        entity.SourceType = request.SourceType;
        entity.DestinationType = request.DestinationType;
        entity.DestinationUrl = request.DestinationUrl;
        entity.IsActive = request.IsActive;
        entity.EnableInput = request.EnableInput;
        entity.EnableOutput = request.EnableOutput;
        entity.EnableMessageCapture = request.EnableMessageCapture;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Version++;

        // Update field mappings
        entity.FieldMappings = request.FieldMappings?.Select(fm => new FieldMappingEntity
        {
            Id = fm.Id == Guid.Empty ? Guid.NewGuid() : fm.Id,
            Source = fm.Source,
            Destination = fm.Destination,
            Order = fm.Order,
            Transformers = fm.Transformers?.Select(t => new TransformerConfigEntity
            {
                Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                Name = t.Name,
                Order = t.Order,
                Arguments = t.Arguments == null ? null : JsonSerializer.Serialize(t.Arguments)
            }).ToList() ?? []
        }).ToList() ?? [];

        // Update static values
        entity.StaticValues = request.StaticValues?.Select(kv => new StaticValueEntity
        {
            Id = Guid.NewGuid(),
            Key = kv.Key,
            Value = kv.Value
        }).ToList() ?? [];

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated integration {IntegrationId} to version {Version}", entity.Id, entity.Version);

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted integration {IntegrationId} with name {Name}", entity.Id, entity.Name);

        return true;
    }

    private static IntegrationDto MapToDto(IntegrationMappingEntity entity)
    {
        return new IntegrationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Endpoint = entity.Endpoint,
            SourceType = entity.SourceType,
            DestinationType = entity.DestinationType,
            DestinationUrl = entity.DestinationUrl,
            IsActive = entity.IsActive,
            EnableInput = entity.EnableInput,
            EnableOutput = entity.EnableOutput,
            EnableMessageCapture = entity.EnableMessageCapture,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            FieldMappings = entity.FieldMappings?.Select(fm => new FieldMappingDto
            {
                Id = fm.Id,
                Source = fm.Source,
                Destination = fm.Destination,
                Order = fm.Order,
                Transformers = fm.Transformers?.Select(t => new TransformerDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Order = t.Order,
                    Arguments = string.IsNullOrEmpty(t.Arguments)
                        ? null
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(t.Arguments)
                }).ToList()
            }).ToList(),
            StaticValues = entity.StaticValues?.ToDictionary(sv => sv.Key, sv => sv.Value),
            SoapConfig = entity.SoapConfig == null ? null : new SoapConfigDto
            {
                BodyWrapperFieldXpath = entity.SoapConfig.BodyWrapperFieldXPath,
                Fields = entity.SoapConfig.Fields?.Select(f => new SoapFieldDto
                {
                    FieldType = f.FieldType,
                    Xpath = f.XPath,
                    Source = f.Source,
                    Namespace = f.Namespace,
                    Prefix = f.Prefix,
                    Attributes = string.IsNullOrEmpty(f.Attributes)
                        ? null
                        : JsonSerializer.Deserialize<Dictionary<string, string>>(f.Attributes),
                    Order = f.Order
                }).ToList()
            }
        };
    }

    private static IntegrationMappingEntity MapToEntity(CreateIntegrationRequest request)
    {
        return new IntegrationMappingEntity
        {
            Name = request.Name,
            Endpoint = request.Endpoint,
            SourceType = request.SourceType,
            DestinationType = request.DestinationType,
            DestinationUrl = request.DestinationUrl,
            IsActive = request.IsActive,
            EnableInput = request.EnableInput,
            EnableOutput = request.EnableOutput,
            EnableMessageCapture = request.EnableMessageCapture,
            FieldMappings = request.FieldMappings?.Select(fm => new FieldMappingEntity
            {
                Id = Guid.NewGuid(),
                Source = fm.Source,
                Destination = fm.Destination,
                Order = fm.Order,
                Transformers = fm.Transformers?.Select(t => new TransformerConfigEntity
                {
                    Id = Guid.NewGuid(),
                    Name = t.Name,
                    Order = t.Order,
                    Arguments = t.Arguments == null ? null : JsonSerializer.Serialize(t.Arguments)
                }).ToList() ?? []
            }).ToList() ?? [],
            StaticValues = request.StaticValues?.Select(kv => new StaticValueEntity
            {
                Id = Guid.NewGuid(),
                Key = kv.Key,
                Value = kv.Value
            }).ToList() ?? [],
            SoapConfig = request.SoapConfig == null ? null : new SoapConfigEntity
            {
                Id = Guid.NewGuid(),
                BodyWrapperFieldXPath = request.SoapConfig.BodyWrapperFieldXpath,
                Fields = request.SoapConfig.Fields?.Select(f => new SoapFieldEntity
                {
                    Id = Guid.NewGuid(),
                    FieldType = f.FieldType,
                    XPath = f.Xpath,
                    Source = f.Source,
                    Namespace = f.Namespace,
                    Prefix = f.Prefix,
                    Attributes = f.Attributes == null ? null : JsonSerializer.Serialize(f.Attributes),
                    Order = f.Order
                }).ToList() ?? []
            }
        };
    }
}

/// <summary>
/// Interface for integration service.
/// </summary>
public interface IIntegrationService
{
    Task<IEnumerable<IntegrationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IntegrationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IntegrationDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IntegrationDto?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<IntegrationDto> CreateAsync(CreateIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDto> UpdateAsync(Guid id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
