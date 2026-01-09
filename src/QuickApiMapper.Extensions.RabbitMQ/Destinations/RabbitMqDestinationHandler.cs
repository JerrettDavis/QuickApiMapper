using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using System.Text;
using System.Xml.Linq;
using QuickApiMapper.Contracts;
using QuickApiMapper.Application.Destinations;

namespace QuickApiMapper.Extensions.RabbitMQ.Destinations;

/// <summary>
/// Destination handler for publishing messages to RabbitMQ exchanges or queues.
/// </summary>
public class RabbitMqDestinationHandler : IDestinationHandler
{
    private readonly ILogger<RabbitMqDestinationHandler> _logger;
    private readonly IConnectionFactory _connectionFactory;

    public RabbitMqDestinationHandler(
        ILogger<RabbitMqDestinationHandler> logger,
        IConnectionFactory connectionFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public bool CanHandle(string destinationType)
    {
        return destinationType.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase) ||
               destinationType.Equals("AMQP", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(
        IntegrationMapping integration,
        JObject? outJson,
        XDocument? outXml,
        HttpRequest req,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            // Determine message body from JSON or XML output
            string messageBody;
            string contentType;

            if (outJson != null)
            {
                messageBody = outJson.ToString();
                contentType = "application/json";
            }
            else if (outXml != null)
            {
                messageBody = outXml.ToString();
                contentType = "application/xml";
            }
            else
            {
                _logger.LogError("No output data to send to RabbitMQ");
                resp.StatusCode = StatusCodes.Status500InternalServerError;
                await resp.WriteAsync("No output data available", cancellationToken);
                return;
            }

            // Parse destination URL
            // Format: rabbitmq://exchange/routingkey or rabbitmq://queue
            var (exchangeName, routingKey, queueName) = ParseDestinationUrl(integration.DestinationUrl);

            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            // If publishing to a queue directly, declare it
            if (!string.IsNullOrEmpty(queueName))
            {
                channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
            }

            // Prepare message
            var body = Encoding.UTF8.GetBytes(messageBody);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = contentType;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Add custom headers from static values
            if (integration.StaticValues != null && integration.StaticValues.Any())
            {
                properties.Headers = integration.StaticValues.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value);
            }

            // Publish message
            if (!string.IsNullOrEmpty(queueName))
            {
                // Publish directly to queue
                channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Published message to RabbitMQ queue: {Queue}", queueName);
            }
            else
            {
                // Publish to exchange
                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Published message to RabbitMQ exchange: {Exchange} with routing key: {RoutingKey}",
                    exchangeName, routingKey);
            }

            resp.StatusCode = StatusCodes.Status200OK;
            await resp.WriteAsync($"Message published to RabbitMQ", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message to RabbitMQ");
            resp.StatusCode = StatusCodes.Status500InternalServerError;
            await resp.WriteAsync($"Error: {ex.Message}", cancellationToken);
        }
    }

    private (string exchangeName, string routingKey, string queueName) ParseDestinationUrl(string destinationUrl)
    {
        // Expected formats:
        // rabbitmq://exchange/routingkey
        // rabbitmq://queue
        // exchange/routingkey
        // queuename

        if (string.IsNullOrWhiteSpace(destinationUrl))
            return (string.Empty, string.Empty, string.Empty);

        var url = destinationUrl;

        // Remove protocol prefix if present
        if (url.StartsWith("rabbitmq://", StringComparison.OrdinalIgnoreCase))
        {
            url = url.Substring("rabbitmq://".Length);
        }
        else if (url.StartsWith("amqp://", StringComparison.OrdinalIgnoreCase))
        {
            url = url.Substring("amqp://".Length);
        }

        url = url.TrimEnd('/');

        // Check if it contains a slash (exchange/routingkey)
        if (url.Contains('/'))
        {
            var parts = url.Split('/', 2);
            return (parts[0], parts[1], string.Empty);
        }

        // Otherwise, treat it as a queue name
        return (string.Empty, string.Empty, url);
    }
}
