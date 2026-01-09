namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing Azure Service Bus-specific configuration for an integration (Phase 2 feature).
/// </summary>
public class ServiceBusConfigEntity
{
    /// <summary>
    /// Unique identifier for the Service Bus configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping.
    /// </summary>
    public Guid IntegrationMappingId { get; set; }

    /// <summary>
    /// Service Bus connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Source queue name for receiving messages.
    /// </summary>
    public string? SourceQueue { get; set; }

    /// <summary>
    /// Source topic name for receiving messages.
    /// </summary>
    public string? SourceTopic { get; set; }

    /// <summary>
    /// Source subscription name (when using topics).
    /// </summary>
    public string? SourceSubscription { get; set; }

    /// <summary>
    /// Destination queue name for sending messages.
    /// </summary>
    public string? DestinationQueue { get; set; }

    /// <summary>
    /// Destination topic name for sending messages.
    /// </summary>
    public string? DestinationTopic { get; set; }

    /// <summary>
    /// Message type expected/sent (e.g., "JSON", "XML").
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Whether to auto-complete messages after processing.
    /// </summary>
    public bool AutoComplete { get; set; } = true;

    /// <summary>
    /// Maximum concurrent message processing.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 1;

    // Navigation properties

    /// <summary>
    /// Parent integration mapping.
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }
}
