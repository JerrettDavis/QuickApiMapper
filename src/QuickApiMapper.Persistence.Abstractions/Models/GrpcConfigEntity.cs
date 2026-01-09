namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing gRPC-specific configuration for an integration (Phase 2 feature).
/// </summary>
public class GrpcConfigEntity
{
    /// <summary>
    /// Unique identifier for the gRPC configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping.
    /// </summary>
    public Guid IntegrationMappingId { get; set; }

    /// <summary>
    /// Name of the gRPC service (e.g., "UserService").
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Name of the gRPC method (e.g., "CreateUser").
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Path to the .proto file defining the service.
    /// </summary>
    public string? ProtoFile { get; set; }

    /// <summary>
    /// Package name in the .proto file (e.g., "user.v1").
    /// </summary>
    public string? Package { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent integration mapping.
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }
}
