using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.RabbitMQ.Destinations;
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

        // Register destination handler (both as interface and concrete type for testing)
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
                        factory,
                        queueConfig.QueueName,
                        queueConfig.ExchangeName,
                        queueConfig.RoutingKey);
                });
            }
        }

        return services;
    }
}

/// <summary>
/// Options for configuring RabbitMQ support in QuickApiMapper.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ host name or IP address.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port. Default is 5672 (5671 for SSL).
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host. Default is "/".
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication. Default is "guest".
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for authentication. Default is "guest".
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Whether to use SSL/TLS connection.
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Input queues to consume from.
    /// Used for creating background workers.
    /// </summary>
    public List<RabbitMqQueueConfig>? InputQueues { get; set; }

    /// <summary>
    /// Maximum number of concurrent message processing calls per consumer.
    /// Default is 10.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;
}

/// <summary>
/// Configuration for a RabbitMQ queue to consume from.
/// </summary>
public class RabbitMqQueueConfig
{
    /// <summary>
    /// Queue name to consume from.
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Exchange name to bind to (optional).
    /// </summary>
    public string? ExchangeName { get; set; }

    /// <summary>
    /// Routing key for exchange binding (optional).
    /// </summary>
    public string? RoutingKey { get; set; }
}
