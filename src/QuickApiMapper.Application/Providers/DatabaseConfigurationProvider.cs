using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Application.Providers;

/// <summary>
/// Configuration provider that reads from a database via repositories.
/// Demonstrates extensibility - same interface, completely different data source.
/// Future providers could use Kafka, event stores, HTTP APIs, etc.
/// Uses source-generated logging for high performance.
/// </summary>
public partial class DatabaseConfigurationProvider : IIntegrationConfigurationProvider
{
    private readonly IIntegrationMappingRepository _repository;
    private readonly ILogger<DatabaseConfigurationProvider> _logger;

    public DatabaseConfigurationProvider(
        IIntegrationMappingRepository repository,
        ILogger<DatabaseConfigurationProvider> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllActiveAsync(cancellationToken);
        var integrations = entities.Select(MapEntityToContract).ToList();

        LogIntegrationsLoaded(integrations.Count);

        return integrations;
    }

    public async Task<IntegrationMapping?> GetIntegrationByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            LogInvalidGuidFormat(id);
            return null;
        }

        var entity = await _repository.GetByIdAsync(guid, cancellationToken);

        if (entity != null)
        {
            LogIntegrationFoundById(id);
            return MapEntityToContract(entity);
        }

        LogIntegrationNotFoundById(id);
        return null;
    }

    public async Task<IntegrationMapping?> GetIntegrationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByNameAsync(name, cancellationToken);

        if (entity != null)
        {
            LogIntegrationFoundByName(name);
            return MapEntityToContract(entity);
        }

        LogIntegrationNotFoundByName(name);
        return null;
    }

    public async Task<IntegrationMapping?> GetIntegrationByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEndpointAsync(endpoint, cancellationToken);

        if (entity != null)
        {
            LogIntegrationFoundByEndpoint(endpoint);
            return MapEntityToContract(entity);
        }

        LogIntegrationNotFoundByEndpoint(endpoint);
        return null;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetGlobalStaticValuesAsync(cancellationToken);
        var dictionary = entities.ToDictionary(e => e.Key, e => e.Value);

        LogGlobalStaticValuesLoaded(dictionary.Count);

        return dictionary;
    }

    public Task<IReadOnlyDictionary<string, string>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        // For database provider, namespaces could be stored in a separate table
        // For now, return empty dictionary (can be extended later)
        LogNamespacesNotImplemented();
        return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }

    /// <summary>
    /// Maps a persistence entity to the contract model.
    /// This demonstrates the separation between storage and domain models.
    /// </summary>
    private IntegrationMapping MapEntityToContract(IntegrationMappingEntity entity)
    {
        // Map field mappings (already ordered at database level)
        var fieldMappings = entity.FieldMappings?
            .Select(fm => new FieldMapping(
                fm.Source,
                fm.Destination,
                fm.Transformers?
                    .Select(t => new Transformer(
                        t.Name,
                        ParseTransformerArguments(t.Arguments)))
                    .ToList() ?? []
            ))
            .ToList() ?? [];

        // Map static values specific to this integration
        var staticValues = entity.StaticValues?
            .Where(sv => !sv.IsGlobal)
            .ToDictionary(sv => sv.Key, sv => sv.Value) ?? new Dictionary<string, string>();

        // Map SOAP configuration if present (already ordered at database level)
        SoapConfig? soapConfig = null;
        if (entity.SoapConfig != null)
        {
            var headerFields = entity.SoapConfig.Fields?
                .Where(f => f.FieldType == "Header")
                .Select(f => new SoapFieldConfig(
                    f.XPath,
                    f.Source ?? string.Empty,
                    null, // Transformers handled at field mapping level
                    f.Namespace,
                    f.Prefix,
                    f.Attributes != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(f.Attributes) : null
                ))
                .ToList() ?? [];

            var bodyFields = entity.SoapConfig.Fields?
                .Where(f => f.FieldType == "Body")
                .Select(f => new SoapFieldConfig(
                    f.XPath,
                    f.Source ?? string.Empty,
                    null,
                    f.Namespace,
                    f.Prefix,
                    f.Attributes != null ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(f.Attributes) : null
                ))
                .ToList() ?? [];

            soapConfig = new SoapConfig(
                headerFields.Count > 0 ? headerFields : null,
                bodyFields.Count > 0 ? bodyFields : null,
                entity.SoapConfig.BodyWrapperFieldXPath
            );
        }

        return new IntegrationMapping(
            entity.Name,
            entity.Endpoint,
            entity.SourceType,
            entity.DestinationType,
            entity.DestinationUrl,
            null, // PayloadArguments - can be extended later
            entity.DispatchFor,
            staticValues.Count > 0 ? staticValues : null,
            fieldMappings.Count > 0 ? fieldMappings : null,
            null, // SoapHeaderXml - deprecated in favor of SoapConfig
            soapConfig
        );
    }

    /// <summary>
    /// Parses JSON transformer arguments into a dictionary.
    /// </summary>
    private static IReadOnlyDictionary<string, string?>? ParseTransformerArguments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
        }
        catch
        {
            // If parsing fails, return null (transformer will use default behavior)
            return null;
        }
    }
}
