using System.ComponentModel.DataAnnotations;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Contracts;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.gRPC.Destinations;
using QuickApiMapper.Extensions.gRPC.Options;
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

        // Register gRPC destination handler with keyed service for modern .NET
        services.AddKeyedSingleton<IDestinationHandler, GrpcDestinationHandler>("gRPC");
        services.AddKeyedSingleton<IDestinationHandler, GrpcDestinationHandler>("GRPC"); // Case variation

        // Also register non-keyed for backward compatibility
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

            // Validate options
            ValidateOptions(options);

            if (options.EnableReflection)
            {
                services.AddGrpcReflection();
            }
        }

        return services;
    }

    private static void ValidateOptions(GrpcServiceOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Invalid GrpcServiceOptions configuration: {errors}");
        }
    }
}
