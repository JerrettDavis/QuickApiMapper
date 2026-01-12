using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.RabbitMQ.Destinations;
using QuickApiMapper.Extensions.RabbitMQ.Options;
using QuickApiMapper.Extensions.RabbitMQ.Workers;

namespace QuickApiMapper.Extensions.RabbitMQ.Extensions;

/// <summary>
/// Extension methods for registering RabbitMQ support in QuickApiMapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ protocol support to QuickApiMapper.
    /// Registers destination handlers and background workers for message processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="hostName">RabbitMQ host name.</param>
    /// <param name="configureOptions">Optional action to configure RabbitMQ options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRabbitMqSupport(
        this IServiceCollection services,
        string hostName,
        Action<RabbitMqOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            throw new ArgumentException("RabbitMQ host name is required", nameof(hostName));
        }

        // Configure options
        var options = new RabbitMqOptions
        {
            HostName = hostName
        };
        configureOptions?.Invoke(options);

        // Validate options
        ValidateOptions(options);

        // Register RabbitMQ connection factory as singleton
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            // Apply SSL settings if configured
            if (options.UseSsl)
            {
                factory.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = options.HostName
                };
            }

            return factory;
        });

        // Register destination handler with keyed service for modern .NET
        services.AddKeyedSingleton<IDestinationHandler, RabbitMqDestinationHandler>("RabbitMQ");
        services.AddKeyedSingleton<IDestinationHandler, RabbitMqDestinationHandler>("RABBITMQ"); // Case variation

        // Also register non-keyed for backward compatibility (and concrete type for testing)
        services.AddSingleton<RabbitMqDestinationHandler>();
        services.AddSingleton<IDestinationHandler>(sp => sp.GetRequiredService<RabbitMqDestinationHandler>());

        // Register background consumers if queues are specified
        if (options.InputQueues != null)
        {
            foreach (var queue in options.InputQueues)
            {
                var queueConfig = queue; // Capture for closure
                services.AddHostedService(sp =>
                {
                    var factory = sp.GetRequiredService<IConnectionFactory>();
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqConsumer>>();
                    return new RabbitMqConsumer(
                        logger,
                        sp,
                        factory,
                        queueConfig.QueueName,
                        queueConfig.ExchangeName,
                        queueConfig.RoutingKey,
                        options.PrefetchCount,
                        queueConfig.DefaultIntegrationName);
                });
            }
        }

        return services;
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Invalid RabbitMqOptions configuration: {errors}");
        }

        // Validate nested queue configurations
        if (options.InputQueues != null)
        {
            foreach (var queueConfig in options.InputQueues)
            {
                var queueContext = new ValidationContext(queueConfig);
                var queueResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(queueConfig, queueContext, queueResults, validateAllProperties: true))
                {
                    var queueErrors = string.Join("; ", queueResults.Select(r => r.ErrorMessage));
                    throw new ArgumentException($"Invalid RabbitMqQueueConfig: {queueErrors}");
                }
            }
        }
    }
}
