using System.ComponentModel.DataAnnotations;

namespace QuickApiMapper.Extensions.gRPC.Options;

/// <summary>
/// Options for configuring gRPC support in QuickApiMapper.
/// </summary>
public class GrpcServiceOptions
{
    /// <summary>
    /// Enable gRPC server reflection for dynamic service discovery.
    /// Useful for testing with tools like grpcurl or Postman.
    /// </summary>
    public bool EnableReflection { get; set; }

    /// <summary>
    /// Maximum message size in bytes for gRPC requests/responses.
    /// Default is 4 MB.
    /// </summary>
    [Range(1024, 104857600, ErrorMessage = "MaxMessageSize must be between 1 KB and 100 MB")]
    public int MaxMessageSize { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Connection timeout for downstream gRPC services.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:05:00", ErrorMessage = "ConnectionTimeout must be between 1 second and 5 minutes")]
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable detailed error messages in gRPC responses.
    /// Should be disabled in production for security.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }
}
