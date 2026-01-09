# Configuration Providers - Extensibility Guide

This directory contains configuration provider implementations demonstrating QuickApiMapper's extensible architecture.

## Implemented Providers

### 1. FileBasedConfigurationProvider
- **Source**: appsettings.json (via IConfiguration)
- **Use Case**: Development, backward compatibility, simple deployments
- **Features**: Fast, no external dependencies

### 2. DatabaseConfigurationProvider
- **Source**: PostgreSQL/SQLite via EF Core repositories
- **Use Case**: Production deployments with dynamic configuration
- **Features**: CRUD operations, versioning, audit trails

### 3. CachedConfigurationProvider
- **Pattern**: Decorator
- **Use Case**: Performance optimization for any provider
- **Features**: Configurable expiration, cache invalidation

## Adding New Providers

The `IConfigurationProvider` interface supports ANY data source. Here are examples:

### Example: Kafka Provider (Event-Driven Configuration)

```csharp
public class KafkaConfigurationProvider : IObservableConfigurationProvider
{
 private readonly IConsumer<string, string> _consumer;
 private readonly ConcurrentDictionary<string, IntegrationMapping> _integrations = new();

 public KafkaConfigurationProvider(IConsumer<string, string> kafkaConsumer)
 {
 _consumer = kafkaConsumer;
 // Subscribe to topic: "quickapimapper.config.integrations"
 _consumer.Subscribe("quickapimapper.config.integrations");

 // Start background task to consume messages
 Task.Run(ConsumeConfigurationChanges);
 }

 public Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(
 CancellationToken cancellationToken = default)
 {
 // Return current snapshot from in-memory dictionary
 return Task.FromResult<IEnumerable<IntegrationMapping>>(
 _integrations.Values.ToList());
 }

 public IDisposable SubscribeToChanges(
 Action<ConfigurationChangeNotification> onConfigurationChanged,
 CancellationToken cancellationToken = default)
 {
 // Real-time notifications when Kafka messages arrive
 // Implementation details...
 }

 private async Task ConsumeConfigurationChanges()
 {
 while (true)
 {
 var result = _consumer.Consume();
 var integration = JsonSerializer.Deserialize<IntegrationMapping>(result.Message.Value);

 // Update in-memory cache
 _integrations.AddOrUpdate(integration.Name, integration, (k, v) => integration);

 // Notify subscribers
 NotifyChange(new ConfigurationChangeNotification(
 ConfigurationChangeType.Updated,
 integration.Name,
 integration));
 }
 }
}
```

### Example: Azure App Configuration Provider

```csharp
public class AzureAppConfigurationProvider : IObservableConfigurationProvider
{
 private readonly ConfigurationClient _client;

 public AzureAppConfigurationProvider(string connectionString)
 {
 _client = new ConfigurationClient(connectionString);
 }

 public async Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(
 CancellationToken cancellationToken = default)
 {
 var settings = _client.GetConfigurationSettingsAsync(
 new SettingSelector { KeyFilter = "QuickApiMapper:Integrations:*" },
 cancellationToken);

 var integrations = new List<IntegrationMapping>();
 await foreach (var setting in settings)
 {
 var integration = JsonSerializer.Deserialize<IntegrationMapping>(setting.Value);
 if (integration != null)
 {
 integrations.Add(integration);
 }
 }

 return integrations;
 }

 public IDisposable SubscribeToChanges(
 Action<ConfigurationChangeNotification> onConfigurationChanged,
 CancellationToken cancellationToken = default)
 {
 // Use Azure App Configuration's push notifications or polling
 // Implementation details...
 }
}
```

### Example: Consul KV Provider (Service Discovery)

```csharp
public class ConsulConfigurationProvider : IConfigurationProvider
{
 private readonly IConsulClient _consul;

 public ConsulConfigurationProvider(IConsulClient consulClient)
 {
 _consul = consulClient;
 }

 public async Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(
 CancellationToken cancellationToken = default)
 {
 var keys = await _consul.KV.List("quickapimapper/integrations/", cancellationToken);

 var integrations = new List<IntegrationMapping>();
 foreach (var kvPair in keys.Response)
 {
 var json = System.Text.Encoding.UTF8.GetString(kvPair.Value);
 var integration = JsonSerializer.Deserialize<IntegrationMapping>(json);
 if (integration != null)
 {
 integrations.Add(integration);
 }
 }

 return integrations;
 }

 // Implement other methods...
}
```

### Example: REST API Provider (Remote Configuration Service)

```csharp
public class HttpConfigurationProvider : IConfigurationProvider
{
 private readonly HttpClient _httpClient;
 private readonly string _baseUrl;

 public HttpConfigurationProvider(HttpClient httpClient, string baseUrl)
 {
 _httpClient = httpClient;
 _baseUrl = baseUrl;
 }

 public async Task<IEnumerable<IntegrationMapping>> GetAllActiveIntegrationsAsync(
 CancellationToken cancellationToken = default)
 {
 var response = await _httpClient.GetAsync(
 $"{_baseUrl}/api/integrations",
 cancellationToken);

 response.EnsureSuccessStatusCode();

 var json = await response.Content.ReadAsStringAsync(cancellationToken);
 return JsonSerializer.Deserialize<List<IntegrationMapping>>(json)
 ?? Enumerable.Empty<IntegrationMapping>();
 }

 // Implement other methods...
}
```

## Combining Providers

Providers can be combined using composition patterns:

### Composite Provider (Fallback Chain)

```csharp
public class CompositeConfigurationProvider : IConfigurationProvider
{
 private readonly IConfigurationProvider _primary;
 private readonly IConfigurationProvider _fallback;

 public async Task<IntegrationMapping?> GetIntegrationByNameAsync(
 string name,
 CancellationToken cancellationToken = default)
 {
 // Try primary first (e.g., database)
 var integration = await _primary.GetIntegrationByNameAsync(name, cancellationToken);

 // Fall back to secondary (e.g., file)
 if (integration == null)
 {
 integration = await _fallback.GetIntegrationByNameAsync(name, cancellationToken);
 }

 return integration;
 }
}
```

## Key Principles

1. **Single Responsibility**: Each provider focuses on one data source
2. **Decorator Pattern**: Cross-cutting concerns (caching, logging) via decorators
3. **Async/Await**: All operations support cancellation and async I/O
4. **Immutability**: Contract models are immutable records
5. **Observability**: IObservableConfigurationProvider for real-time updates

## Registration Example

```csharp
services.AddSingleton<IConfigurationProvider>(sp =>
{
 IConfigurationProvider provider;

 if (useDatabase)
 {
 // Database provider
 provider = new DatabaseConfigurationProvider(
 sp.GetRequiredService<IIntegrationMappingRepository>(),
 sp.GetRequiredService<ILogger<DatabaseConfigurationProvider>>());
 }
 else
 {
 // File provider (backward compatibility)
 provider = new FileBasedConfigurationProvider(
 sp.GetRequiredService<IConfiguration>(),
 sp.GetRequiredService<ILogger<FileBasedConfigurationProvider>>());
 }

 // Wrap with caching decorator
 return new CachedConfigurationProvider(
 provider,
 sp.GetRequiredService<IMemoryCache>(),
 sp.GetRequiredService<ILogger<CachedConfigurationProvider>>(),
 TimeSpan.FromMinutes(5));
});
```

This design allows QuickApiMapper to integrate with ANY configuration source without changing core application logic!
