namespace QuickApiMapper.Contracts;

/// <summary>
/// Extensible provider interface for retrieving integration mapping configurations.
/// This abstraction supports ANY data source: files, databases, Kafka, event streams, HTTP APIs, etc.
///
/// Implementation examples:
/// - FileBasedConfigurationProvider: Reads from appsettings.json
/// - DatabaseConfigurationProvider: Reads from EF Core repositories
/// - KafkaConfigurationProvider: Reads from Kafka topics
/// - EventStoreConfigurationProvider: Reads from event sourcing store
/// - HttpConfigurationProvider: Fetches from remote configuration API
/// - CompositeConfigurationProvider: Combines multiple sources with fallback logic
/// </summary>
public interface IIntegrationConfigurationProvider
{
    /// <summary>
    /// Gets all active integration mappings from the configuration source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active integration mappings.</returns>
    Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific integration mapping by its unique identifier.
    /// </summary>
    /// <param name="id">The integration mapping identifier (could be GUID, name, or any unique key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMapping?> GetIntegrationByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific integration mapping by its name.
    /// </summary>
    /// <param name="name">The integration name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMapping?> GetIntegrationByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific integration mapping by its endpoint path.
    /// </summary>
    /// <param name="endpoint">The endpoint path (e.g., "/CustomerIntegration").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMapping?> GetIntegrationByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all global static values (not specific to any integration).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of global static values.</returns>
    Task<IReadOnlyDictionary<string, string>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all XML namespaces used across integrations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of namespace prefixes and URIs.</returns>
    Task<IReadOnlyDictionary<string, string>> GetNamespacesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional interface for configuration providers that support change notifications.
/// Useful for sources like Kafka, event streams, or file watchers that can push updates.
/// </summary>
public interface IObservableIntegrationConfigurationProvider : IIntegrationConfigurationProvider
{
    /// <summary>
    /// Subscribes to configuration change notifications.
    /// </summary>
    /// <param name="onConfigurationChanged">Callback invoked when configuration changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable subscription handle.</returns>
    IDisposable SubscribeToChanges(
        Action<ConfigurationChangeNotification> onConfigurationChanged,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification about a configuration change.
/// </summary>
public record ConfigurationChangeNotification(
    ConfigurationChangeType ChangeType,
    string IntegrationId,
    IntegrationMapping? UpdatedIntegration = null);

/// <summary>
/// Type of configuration change.
/// </summary>
public enum ConfigurationChangeType
{
    /// <summary>
    /// A new integration was added.
    /// </summary>
    Added,

    /// <summary>
    /// An existing integration was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// An integration was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// Global configuration (static values, namespaces) changed.
    /// </summary>
    GlobalChanged
}
