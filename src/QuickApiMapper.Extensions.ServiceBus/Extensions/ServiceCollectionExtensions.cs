using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.ServiceBus.Destinations;
using QuickApiMapper.Extensions.ServiceBus.Workers;

namespace QuickApiMapper.Extensions.ServiceBus.Extensions;

/// <summary>
/// Extension methods for registering Azure Service Bus support in QuickApiMapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Service Bus protocol support to QuickApiMapper.
    /// Registers destination handlers and background workers for message processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Azure Service Bus connection string.</param>
    /// <param name="configureOptions">Optional action to configure Service Bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceBusSupport(
        this IServiceCollection services,
        string connectionString,
        Action<ServiceBusOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Service Bus connection string is required", nameof(connectionString));
        }

        // Configure options
        var options = new ServiceBusOptions
        {
            ConnectionString = connectionString
        };
        configureOptions?.Invoke(options);

        // Register Service Bus client as singleton
        services.AddSingleton(sp => new ServiceBusClient(connectionString, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = options.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30)
            }
        }));

        // Register destination handler
        services.AddSingleton<IDestinationHandler, ServiceBusDestinationHandler>();

        // Register background workers if queues are specified
        if (options.InputQueues != null)
        {
            foreach (var queue in options.InputQueues)
            {
                var queueConfig = queue; // Capture for closure
                services.AddHostedService(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceBusWorker>>();
                    return new ServiceBusWorker(logger, client, queueConfig.QueueOrTopicName, queueConfig.SubscriptionName);
                });
            }
        }

        return services;
    }
}

/// <summary>
/// Options for configuring Azure Service Bus support in QuickApiMapper.
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of retry attempts for failed operations.
    /// Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum number of concurrent message processing calls.
    /// Default is 10.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 10;

    /// <summary>
    /// Input queues or topics to listen to.
    /// Used for creating background workers.
    /// </summary>
    public List<ServiceBusQueueConfig>? InputQueues { get; set; }

    /// <summary>
    /// Whether to automatically complete messages after successful processing.
    /// Default is false (manual completion).
    /// </summary>
    public bool AutoCompleteMessages { get; set; } = false;
}

/// <summary>
/// Configuration for a Service Bus queue or topic subscription.
/// </summary>
public class ServiceBusQueueConfig
{
    /// <summary>
    /// Queue name or topic name.
    /// </summary>
    public string QueueOrTopicName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription name (only for topics).
    /// Leave null for queues.
    /// </summary>
    public string? SubscriptionName { get; set; }
}
