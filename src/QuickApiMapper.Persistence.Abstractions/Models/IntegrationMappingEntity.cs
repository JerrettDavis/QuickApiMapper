namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing an integration mapping configuration stored in the database.
/// </summary>
public class IntegrationMappingEntity
{
    /// <summary>
    /// Unique identifier for the integration mapping.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the integration (e.g., "CustomerIntegration", "VendorIntegration").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// HTTP endpoint path for the integration (e.g., "/CustomerIntegration").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Source data format (e.g., "JSON", "XML", "SOAP", "gRPC").
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Destination data format (e.g., "JSON", "XML", "SOAP", "gRPC").
    /// </summary>
    public string DestinationType { get; set; } = string.Empty;

    /// <summary>
    /// URL of the downstream API to forward transformed data to.
    /// </summary>
    public string DestinationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional dispatch configuration for routing.
    /// </summary>
    public string? DispatchFor { get; set; }

    /// <summary>
    /// Indicates whether this integration is active and should be loaded.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Enables or disables input processing for this integration.
    /// When false, incoming requests return 503 Service Unavailable.
    /// </summary>
    public bool EnableInput { get; set; } = true;

    /// <summary>
    /// Enables or disables output forwarding for this integration.
    /// When false, transformed messages are captured but not forwarded to destination.
    /// </summary>
    public bool EnableOutput { get; set; } = true;

    /// <summary>
    /// Enables or disables message capture for this integration.
    /// </summary>
    public bool EnableMessageCapture { get; set; } = true;

    /// <summary>
    /// Version number for tracking configuration changes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Timestamp when this integration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this integration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this integration.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last updated this integration.
    /// </summary>
    public string? UpdatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Collection of field mappings for this integration.
    /// </summary>
    public List<FieldMappingEntity> FieldMappings { get; set; } = [];

    /// <summary>
    /// Collection of static values for this integration.
    /// </summary>
    public List<StaticValueEntity> StaticValues { get; set; } = [];

    /// <summary>
    /// SOAP-specific configuration if SourceType or DestinationType is SOAP.
    /// </summary>
    public SoapConfigEntity? SoapConfig { get; set; }

    /// <summary>
    /// gRPC-specific configuration (future extension).
    /// </summary>
    public GrpcConfigEntity? GrpcConfig { get; set; }

    /// <summary>
    /// Service Bus-specific configuration (future extension).
    /// </summary>
    public ServiceBusConfigEntity? ServiceBusConfig { get; set; }

    /// <summary>
    /// RabbitMQ-specific configuration (future extension).
    /// </summary>
    public RabbitMqConfigEntity? RabbitMqConfig { get; set; }
}
