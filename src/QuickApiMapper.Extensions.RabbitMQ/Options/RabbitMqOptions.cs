using System.ComponentModel.DataAnnotations;

namespace QuickApiMapper.Extensions.RabbitMQ.Options;

/// <summary>
/// Options for configuring RabbitMQ support in QuickApiMapper.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ host name or IP address.
    /// </summary>
    [Required(ErrorMessage = "RabbitMQ host name is required")]
    [MinLength(1, ErrorMessage = "Host name cannot be empty")]
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port. Default is 5672 (5671 for SSL).
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host. Default is "/".
    /// </summary>
    [Required(ErrorMessage = "Virtual host is required")]
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication. Default is "guest".
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for authentication. Default is "guest".
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
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
    [Range(1, 1000, ErrorMessage = "PrefetchCount must be between 1 and 1000")]
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
    [Required(ErrorMessage = "Queue name is required")]
    [MinLength(1, ErrorMessage = "Queue name cannot be empty")]
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// Exchange name to bind to (optional).
    /// </summary>
    public string? ExchangeName { get; set; }

    /// <summary>
    /// Routing key for exchange binding (optional).
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Default integration name to use if message doesn't specify one.
    /// If not set, integration name must come from message properties or routing key.
    /// </summary>
    public string? DefaultIntegrationName { get; set; }
}
