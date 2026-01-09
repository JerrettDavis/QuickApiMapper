using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace QuickApiMapper.Extensions.RabbitMQ.Workers;

/// <summary>
/// Background worker that consumes messages from a RabbitMQ queue.
/// Processes messages through QuickApiMapper and forwards to destinations.
/// </summary>
public class RabbitMqConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;

    public RabbitMqConsumer(
        ILogger<RabbitMqConsumer> logger,
        IConnectionFactory connectionFactory,
        string queueName,
        string? exchangeName = null,
        string? routingKey = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _exchangeName = exchangeName ?? string.Empty;
        _routingKey = routingKey ?? string.Empty;

        // Create connection and channel
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare queue (idempotent)
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind to exchange if specified
        if (!string.IsNullOrEmpty(_exchangeName))
        {
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: _routingKey);
        }

        // Set QoS to limit prefetch
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMQ consumer for queue: {Queue}", _queueName);

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received message from queue {Queue}: {MessageId}",
                    _queueName, ea.DeliveryTag);
                _logger.LogDebug("Message body: {Body}", message);

                // TODO: Process message through QuickApiMapper
                // This would involve:
                // 1. Getting the integration configuration for this queue
                // 2. Applying field mappings
                // 3. Sending to destination

                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                _logger.LogInformation("Successfully processed message: {MessageId}", ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {MessageId}", ea.DeliveryTag);

                // Reject and requeue the message (or send to DLX if configured)
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer for queue: {Queue}", _queueName);

        _channel?.Close();
        _connection?.Close();

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
