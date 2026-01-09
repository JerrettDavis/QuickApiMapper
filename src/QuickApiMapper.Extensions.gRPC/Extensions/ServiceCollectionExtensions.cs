using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Contracts;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.gRPC.Destinations;
using QuickApiMapper.Extensions.gRPC.Resolvers;
using QuickApiMapper.Extensions.gRPC.Writers;

namespace QuickApiMapper.Extensions.gRPC.Extensions;

/// <summary>
/// Extension methods for registering gRPC support in QuickApiMapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds gRPC protocol support to QuickApiMapper.
    /// Registers resolvers, writers, and destination handlers for gRPC (Protobuf) messages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureGrpc">Optional action to configure gRPC client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGrpcSupport(
        this IServiceCollection services,
        Action<GrpcServiceOptions>? configureGrpc = null)
    {
        // Register gRPC-specific resolvers and writers
        services.AddSingleton<ISourceResolver<IMessage>, GrpcSourceResolver>();
        services.AddSingleton<IDestinationWriter<IMessage>, GrpcDestinationWriter>();

        // Register gRPC destination handler
        services.AddSingleton<IDestinationHandler, GrpcDestinationHandler>();

        // Configure gRPC client factory for downstream calls
        services.AddGrpcClient<object>("QuickApiMapperGrpc", options =>
        {
            // Default configuration
            options.Address = new Uri("http://localhost:5000"); // Placeholder, overridden per integration
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };
        });

        // Apply custom configuration
        if (configureGrpc != null)
        {
            var options = new GrpcServiceOptions();
            configureGrpc(options);

            if (options.EnableReflection)
            {
                services.AddGrpcReflection();
            }
        }

        return services;
    }
}

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
    public int MaxMessageSize { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Connection timeout for downstream gRPC services.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable detailed error messages in gRPC responses.
    /// Should be disabled in production for security.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }
}
