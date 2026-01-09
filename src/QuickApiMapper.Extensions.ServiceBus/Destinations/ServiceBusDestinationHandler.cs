using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using QuickApiMapper.Contracts;
using QuickApiMapper.Application.Destinations;

namespace QuickApiMapper.Extensions.ServiceBus.Destinations;

/// <summary>
/// Destination handler for sending messages to Azure Service Bus queues or topics.
/// </summary>
public class ServiceBusDestinationHandler : IDestinationHandler
{
    private readonly ILogger<ServiceBusDestinationHandler> _logger;
    private readonly ServiceBusClient _client;

    public ServiceBusDestinationHandler(
        ILogger<ServiceBusDestinationHandler> logger,
        ServiceBusClient client)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public bool CanHandle(string destinationType)
    {
        return destinationType.Equals("ServiceBus", StringComparison.OrdinalIgnoreCase) ||
               destinationType.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase);
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
                _logger.LogError("No output data to send to Service Bus");
                resp.StatusCode = StatusCodes.Status500InternalServerError;
                await resp.WriteAsync("No output data available", cancellationToken);
                return;
            }

            // Extract queue/topic name from destination URL
            // Format: servicebus://queuename or servicebus://topicname
            var queueOrTopicName = ExtractQueueOrTopicName(integration.DestinationUrl);

            if (string.IsNullOrEmpty(queueOrTopicName))
            {
                _logger.LogError("Invalid Service Bus destination URL: {Url}", integration.DestinationUrl);
                resp.StatusCode = StatusCodes.Status500InternalServerError;
                await resp.WriteAsync("Invalid Service Bus destination URL", cancellationToken);
                return;
            }

            // Create sender
            await using var sender = _client.CreateSender(queueOrTopicName);

            // Create message
            var message = new ServiceBusMessage(messageBody)
            {
                ContentType = contentType,
                MessageId = Guid.NewGuid().ToString(),
                Subject = $"QuickApiMapper.{integration.Name}"
            };

            // Add custom properties if specified
            if (integration.StaticValues != null)
            {
                foreach (var kvp in integration.StaticValues)
                {
                    message.ApplicationProperties[kvp.Key] = kvp.Value;
                }
            }

            // Send message
            await sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Successfully sent message to Service Bus queue/topic: {QueueOrTopic}",
                queueOrTopicName);

            resp.StatusCode = StatusCodes.Status200OK;
            await resp.WriteAsync($"Message sent to {queueOrTopicName}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Service Bus");
            resp.StatusCode = StatusCodes.Status500InternalServerError;
            await resp.WriteAsync($"Error: {ex.Message}", cancellationToken);
        }
    }

    private string? ExtractQueueOrTopicName(string destinationUrl)
    {
        // Expected formats:
        // servicebus://queuename
        // servicebus://topicname
        // queuename (simple name)

        if (string.IsNullOrWhiteSpace(destinationUrl))
            return null;

        if (destinationUrl.StartsWith("servicebus://", StringComparison.OrdinalIgnoreCase))
        {
            return destinationUrl.Substring("servicebus://".Length).TrimEnd('/');
        }

        // Assume it's just the queue/topic name
        return destinationUrl;
    }
}
