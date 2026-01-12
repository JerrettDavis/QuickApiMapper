namespace QuickApiMapper.Contracts;

/// <summary>
/// C# 14 extension members for IntegrationMapping.
/// Provides computed properties and operators for cleaner, more expressive code.
/// </summary>
public static class IntegrationMappingExtensions
{
    /// <summary>
    /// C# 14 extension property: Gets whether the integration is fully operational (both input and output enabled).
    /// </summary>
    public static bool IsFullyEnabled(this IntegrationMapping integration) =>
        integration.EnableInput && integration.EnableOutput;

    /// <summary>
    /// C# 14 extension property: Gets whether this integration uses SOAP protocol for source or destination.
    /// </summary>
    public static bool IsSoapIntegration(this IntegrationMapping integration) =>
        integration.SourceType.Equals("SOAP", StringComparison.OrdinalIgnoreCase) ||
        integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase) ||
        integration.DestinationType.Equals("XML", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// C# 14 extension property: Gets whether this integration uses gRPC protocol.
    /// </summary>
    public static bool IsGrpcIntegration(this IntegrationMapping integration) =>
        integration.SourceType.Equals("gRPC", StringComparison.OrdinalIgnoreCase) ||
        integration.SourceType.Equals("GRPC", StringComparison.OrdinalIgnoreCase) ||
        integration.DestinationType.Equals("gRPC", StringComparison.OrdinalIgnoreCase) ||
        integration.DestinationType.Equals("GRPC", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// C# 14 extension property: Gets whether this integration uses message queue protocols (RabbitMQ, ServiceBus).
    /// </summary>
    public static bool IsMessageQueueIntegration(this IntegrationMapping integration) =>
        integration.DestinationType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase) ||
        integration.DestinationType.Equals("ServiceBus", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// C# 14 extension property: Gets whether this integration has field mappings configured.
    /// </summary>
    public static bool HasFieldMappings(this IntegrationMapping integration) =>
        integration.Mapping is { Count: > 0 };

    /// <summary>
    /// C# 14 extension property: Gets whether this integration has static values configured.
    /// </summary>
    public static bool HasStaticValues(this IntegrationMapping integration) =>
        integration.StaticValues is { Count: > 0 };

    /// <summary>
    /// C# 14 extension property: Gets the total number of transformers across all field mappings.
    /// </summary>
    public static int TotalTransformerCount(this IntegrationMapping integration) =>
        integration.Mapping?.Sum(m => m.Transformers?.Count ?? 0) ?? 0;

    /// <summary>
    /// C# 14 extension property: Gets the protocol pair as a formatted string (e.g., "JSON → SOAP").
    /// </summary>
    public static string ProtocolFlow(this IntegrationMapping integration) =>
        $"{integration.SourceType} → {integration.DestinationType}";

    /// <summary>
    /// C# 14 extension property: Gets whether this integration is read-only (output disabled).
    /// </summary>
    public static bool IsReadOnly(this IntegrationMapping integration) =>
        integration.EnableInput && !integration.EnableOutput;

    /// <summary>
    /// C# 14 extension property: Gets whether this integration is write-only (input disabled).
    /// </summary>
    public static bool IsWriteOnly(this IntegrationMapping integration) =>
        !integration.EnableInput && integration.EnableOutput;
}
