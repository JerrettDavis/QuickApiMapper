namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing RabbitMQ-specific configuration for an integration (Phase 2 feature).
/// </summary>
public class RabbitMqConfigEntity
{
    /// <summary>
    /// Unique identifier for the RabbitMQ configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping.
    /// </summary>
    public Guid IntegrationMappingId { get; set; }

    /// <summary>
    /// RabbitMQ host name.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// RabbitMQ port number.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Virtual host.
    /// </summary>
    public string? VirtualHost { get; set; }

    /// <summary>
    /// Source exchange name for receiving messages.
    /// </summary>
    public string? SourceExchange { get; set; }

    /// <summary>
    /// Source queue name for receiving messages.
    /// </summary>
    public string? SourceQueue { get; set; }

    /// <summary>
    /// Routing key for source messages.
    /// </summary>
    public string? SourceRoutingKey { get; set; }

    /// <summary>
    /// Destination exchange name for sending messages.
    /// </summary>
    public string? DestinationExchange { get; set; }

    /// <summary>
    /// Routing key for destination messages.
    /// </summary>
    public string? DestinationRoutingKey { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent integration mapping.
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }
}
