using System.ComponentModel.DataAnnotations;

namespace QuickApiMapper.Extensions.ServiceBus.Options;

/// <summary>
/// Options for configuring Azure Service Bus support in QuickApiMapper.
/// </summary>
public class ServiceBusOptions
{
    /// <summary>
    /// Azure Service Bus connection string.
    /// </summary>
    [Required(ErrorMessage = "Service Bus connection string is required")]
    [MinLength(10, ErrorMessage = "Connection string appears to be invalid")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of retry attempts for failed operations.
    /// Default is 3.
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum number of concurrent message processing calls.
    /// Default is 10.
    /// </summary>
    [Range(1, 100, ErrorMessage = "MaxConcurrentCalls must be between 1 and 100")]
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
    [Required(ErrorMessage = "Queue or topic name is required")]
    [MinLength(1, ErrorMessage = "Queue or topic name cannot be empty")]
    public string QueueOrTopicName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription name (only for topics).
    /// Leave null for queues.
    /// </summary>
    public string? SubscriptionName { get; set; }
}
