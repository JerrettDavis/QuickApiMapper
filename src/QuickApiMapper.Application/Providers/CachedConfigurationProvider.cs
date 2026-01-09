using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Providers;

/// <summary>
/// Decorator that adds caching to any IIntegrationConfigurationProvider.
/// Demonstrates extensibility - can wrap file, database, Kafka, or any future provider.
/// Uses Decorator pattern to add cross-cutting concerns without modifying core providers.
/// </summary>
public class CachedConfigurationProvider : IIntegrationConfigurationProvider
{
    private readonly IIntegrationConfigurationProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedConfigurationProvider> _logger;
    private readonly TimeSpan _cacheExpiration;

    private const string AllIntegrationsCacheKey = "QuickApiMapper:AllIntegrations";
    private const string GlobalStaticValuesCacheKey = "QuickApiMapper:GlobalStaticValues";
    private const string NamespacesCacheKey = "QuickApiMapper:Namespaces";
    private const string IntegrationByIdPrefix = "QuickApiMapper:Integration:Id:";
    private const string IntegrationByNamePrefix = "QuickApiMapper:Integration:Name:";
    private const string IntegrationByEndpointPrefix = "QuickApiMapper:Integration:Endpoint:";

    public CachedConfigurationProvider(
        IIntegrationConfigurationProvider innerProvider,
        IMemoryCache cache,
        ILogger<CachedConfigurationProvider> logger,
        TimeSpan? cacheExpiration = null)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(5); // Default 5 minute cache
    }

    public async Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            AllIntegrationsCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for all integrations, loading from provider");

                var integrations = await _innerProvider.GetAllActiveIntegrationsAsync(cancellationToken);
                var list = integrations.ToList(); // Materialize to avoid multiple enumeration

                _logger.LogInformation("Cached {Count} integrations for {Duration}",
                    list.Count, _cacheExpiration);

                return (IEnumerable<IntegrationMapping>)list;
            })
        ?? Enumerable.Empty<IntegrationMapping>();
    }

    public async Task<IntegrationMapping?> GetIntegrationByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{IntegrationByIdPrefix}{id}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for integration ID '{Id}', loading from provider", id);

                var integration = await _innerProvider.GetIntegrationByIdAsync(id, cancellationToken);

                if (integration != null)
                {
                    _logger.LogDebug("Cached integration ID '{Id}' for {Duration}", id, _cacheExpiration);
                }

                return integration;
            });
    }

    public async Task<IntegrationMapping?> GetIntegrationByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{IntegrationByNamePrefix}{name}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for integration name '{Name}', loading from provider", name);

                var integration = await _innerProvider.GetIntegrationByNameAsync(name, cancellationToken);

                if (integration != null)
                {
                    _logger.LogDebug("Cached integration name '{Name}' for {Duration}", name, _cacheExpiration);
                }

                return integration;
            });
    }

    public async Task<IntegrationMapping?> GetIntegrationByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{IntegrationByEndpointPrefix}{endpoint}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for endpoint '{Endpoint}', loading from provider", endpoint);

                var integration = await _innerProvider.GetIntegrationByEndpointAsync(endpoint, cancellationToken);

                if (integration != null)
                {
                    _logger.LogDebug("Cached endpoint '{Endpoint}' for {Duration}", endpoint, _cacheExpiration);
                }

                return integration;
            });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            GlobalStaticValuesCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for global static values, loading from provider");

                var staticValues = await _innerProvider.GetGlobalStaticValuesAsync(cancellationToken);

                _logger.LogDebug("Cached {Count} global static values for {Duration}",
                    staticValues.Count, _cacheExpiration);

                return staticValues;
            })
        ?? new Dictionary<string, string>();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamespacesAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            NamespacesCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                _logger.LogDebug("Cache miss for namespaces, loading from provider");

                var namespaces = await _innerProvider.GetNamespacesAsync(cancellationToken);

                _logger.LogDebug("Cached {Count} namespaces for {Duration}",
                    namespaces.Count, _cacheExpiration);

                return namespaces;
            })
        ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Invalidates all cached configuration data.
    /// Useful when configuration changes are detected (e.g., database updates, file changes).
    /// </summary>
    public void InvalidateCache()
    {
        _cache.Remove(AllIntegrationsCacheKey);
        _cache.Remove(GlobalStaticValuesCacheKey);
        _cache.Remove(NamespacesCacheKey);

        // Note: Individual integration caches will expire naturally
        // For a more aggressive invalidation, consider using MemoryCacheEntryOptions.PostEvictionCallbacks

        _logger.LogInformation("Configuration cache invalidated");
    }
}
