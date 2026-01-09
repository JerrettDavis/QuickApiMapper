using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Providers;

/// <summary>
/// Configuration provider that reads from appsettings.json (or any IConfiguration source).
/// Provides backward compatibility with file-based configuration.
/// </summary>
public class FileBasedConfigurationProvider : IIntegrationConfigurationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileBasedConfigurationProvider> _logger;
    private ApiMappingConfig? _cachedConfig;

    public FileBasedConfigurationProvider(
        IConfiguration configuration,
        ILogger<FileBasedConfigurationProvider> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        var config = GetConfigurationFromFile();
        var integrations = config.Mappings ?? [];

        _logger.LogInformation("Loaded {Count} integrations from configuration file", integrations.Count);

        return Task.FromResult<IEnumerable<IntegrationMapping>>(integrations);
    }

    public Task<IntegrationMapping?> GetIntegrationByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // For file-based provider, ID is the same as Name
        return GetIntegrationByNameAsync(id, cancellationToken);
    }

    public Task<IntegrationMapping?> GetIntegrationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var config = GetConfigurationFromFile();
        var integration = config.Mappings?.FirstOrDefault(m =>
            m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (integration != null)
        {
            _logger.LogDebug("Found integration '{Name}' in configuration file", name);
        }
        else
        {
            _logger.LogWarning("Integration '{Name}' not found in configuration file", name);
        }

        return Task.FromResult(integration);
    }

    public Task<IntegrationMapping?> GetIntegrationByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var config = GetConfigurationFromFile();
        var integration = config.Mappings?.FirstOrDefault(m =>
            m.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase));

        if (integration != null)
        {
            _logger.LogDebug("Found integration with endpoint '{Endpoint}' in configuration file", endpoint);
        }
        else
        {
            _logger.LogWarning("Integration with endpoint '{Endpoint}' not found in configuration file", endpoint);
        }

        return Task.FromResult(integration);
    }

    public Task<IReadOnlyDictionary<string, string>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default)
    {
        var config = GetConfigurationFromFile();
        var staticValues = config.StaticValues ?? new Dictionary<string, string>();

        _logger.LogDebug("Loaded {Count} global static values from configuration file", staticValues.Count);

        return Task.FromResult<IReadOnlyDictionary<string, string>>(staticValues);
    }

    public Task<IReadOnlyDictionary<string, string>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        var config = GetConfigurationFromFile();
        var namespaces = config.Namespaces ?? new Dictionary<string, string>();

        _logger.LogDebug("Loaded {Count} namespaces from configuration file", namespaces.Count);

        return Task.FromResult<IReadOnlyDictionary<string, string>>(namespaces);
    }

    private ApiMappingConfig GetConfigurationFromFile()
    {
        // Cache the configuration to avoid re-parsing on every call
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        _cachedConfig = _configuration
            .GetSection("ApiMapping")
            .Get<ApiMappingConfig>() ?? new ApiMappingConfig(null, null, null);

        _logger.LogInformation("Configuration loaded from file with {Count} integrations",
            _cachedConfig.Mappings?.Count ?? 0);

        return _cachedConfig;
    }
}
