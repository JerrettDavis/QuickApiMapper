using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Contracts;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Models;

namespace QuickApiMapper.Extensions.RabbitMQ.Workers;

/// <summary>
/// Background worker that consumes messages from a RabbitMQ queue.
/// Processes messages through QuickApiMapper and forwards to destinations.
/// </summary>
public class RabbitMqConsumer : BackgroundService
{
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _queueName;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly int _prefetchCount;
    private readonly string? _defaultIntegrationName;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(
        ILogger<RabbitMqConsumer> logger,
        IServiceProvider serviceProvider,
        IConnectionFactory connectionFactory,
        string queueName,
        string? exchangeName = null,
        string? routingKey = null,
        int prefetchCount = 10,
        string? defaultIntegrationName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _exchangeName = exchangeName ?? string.Empty;
        _routingKey = routingKey ?? string.Empty;
        _prefetchCount = prefetchCount;
        _defaultIntegrationName = defaultIntegrationName;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Create connection and channel
#pragma warning disable IDISP003
        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
#pragma warning restore IDISP003

        // Declare queue with dead-letter exchange support
        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = $"{_queueName}.dlx"
        };

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        // Declare dead-letter queue
        await _channel.ExchangeDeclareAsync($"{_queueName}.dlx", "direct", durable: true, cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync($"{_queueName}.dead-letter", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync($"{_queueName}.dead-letter", $"{_queueName}.dlx", _queueName, cancellationToken: cancellationToken);

        // Bind to exchange if specified
        if (!string.IsNullOrEmpty(_exchangeName))
        {
            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: _routingKey,
                cancellationToken: cancellationToken);
        }

        // Set QoS to limit prefetch
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: (ushort)_prefetchCount, global: false, cancellationToken: cancellationToken);

        _logger.LogInformation("RabbitMQ consumer initialized for queue {Queue} with prefetch {Prefetch}",
            _queueName, _prefetchCount);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);

        _logger.LogInformation("Starting RabbitMQ consumer for queue: {Queue}", _queueName);

        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId ?? Guid.NewGuid().ToString();
            var deliveryTag = ea.DeliveryTag;

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received message from queue {Queue}, DeliveryTag: {DeliveryTag}, CorrelationId: {CorrelationId}",
                    _queueName, deliveryTag, correlationId);
                _logger.LogDebug("Message body: {Body}", message);

                // Process message through QuickApiMapper pipeline
                await ProcessMessageAsync(message, ea, correlationId, stoppingToken);

                // Acknowledge the message
                await _channel!.BasicAckAsync(deliveryTag: deliveryTag, multiple: false, cancellationToken: stoppingToken);

                _logger.LogInformation("Successfully processed and acknowledged message: {DeliveryTag}", deliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {DeliveryTag}, CorrelationId: {CorrelationId}",
                    deliveryTag, correlationId);

                // Capture failed message if message capture is available
                await CaptureFailedMessageAsync(correlationId, ex.Message);

                // Reject and send to dead-letter queue (do not requeue to avoid infinite loops)
                await _channel!.BasicNackAsync(deliveryTag: deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);

                _logger.LogWarning("Message {DeliveryTag} rejected and sent to dead-letter queue", deliveryTag);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        // Keep the worker alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    /// <summary>
    /// Processes a message through the QuickApiMapper pipeline.
    /// </summary>
    private async Task ProcessMessageAsync(
        string messageBody,
        BasicDeliverEventArgs eventArgs,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        // Get required services
        var configProvider = services.GetRequiredService<IIntegrationConfigurationProvider>();
        var mappingEngineFactory = services.GetRequiredService<IMappingEngineFactory>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
        var messageCaptureProvider = services.GetService<IMessageCaptureProvider>();

        // Determine which integration to use
        var integrationName = DetermineIntegrationName(eventArgs);
        if (string.IsNullOrEmpty(integrationName))
        {
            throw new InvalidOperationException(
                "Could not determine integration name. Set 'IntegrationName' message property or configure defaultIntegrationName.");
        }

        _logger.LogInformation("Processing message for integration: {Integration}", integrationName);

        // Load integration configuration
        var integration = await configProvider.GetIntegrationByNameAsync(integrationName, cancellationToken);
        if (integration == null)
        {
            throw new InvalidOperationException($"Integration '{integrationName}' not found in configuration.");
        }

        // Check if input is enabled
        if (!integration.EnableInput)
        {
            _logger.LogWarning("Input disabled for integration {Integration}, message rejected", integrationName);
            throw new InvalidOperationException($"Integration '{integrationName}' has input disabled.");
        }

        // Determine source type from message content
        var sourceType = DetermineSourceType(messageBody, integration);
        _logger.LogDebug("Detected source type: {SourceType}", sourceType);

        // Capture input message
        if (messageCaptureProvider != null && integration.EnableMessageCapture)
        {
            await CaptureInputMessageAsync(
                messageCaptureProvider,
                integration,
                messageBody,
                correlationId,
                sourceType);
        }

        // Process based on source and destination types
        object? outputPayload = null;

        if (sourceType == "JSON")
        {
            var inputJson = JObject.Parse(messageBody);

            if (integration.DestinationType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                // JSON to JSON mapping
                var outputJson = new JObject();
                var engine = mappingEngineFactory.CreateEngine<JObject, JObject>();

                var globalStatics = await configProvider.GetGlobalStaticValuesAsync(cancellationToken);
                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputJson,
                    outputJson,
                    integration.StaticValues,
                    globalStatics,
                    services,
                    cancellationToken);

                outputPayload = outputJson;
                await ForwardToDestinationAsync(integration, outputJson, null, httpClientFactory, cancellationToken);
            }
            else if (integration.DestinationType.Equals("XML", StringComparison.OrdinalIgnoreCase) ||
                     integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase))
            {
                // JSON to XML/SOAP mapping
                var outputXml = CreateXmlDocument(integration);
                var engine = mappingEngineFactory.CreateEngine<JObject, XDocument>();

                var globalStatics = await configProvider.GetGlobalStaticValuesAsync(cancellationToken);
                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputJson,
                    outputXml,
                    integration.StaticValues,
                    globalStatics,
                    services,
                    cancellationToken);

                outputPayload = outputXml;
                await ForwardToDestinationAsync(integration, null, outputXml, httpClientFactory, cancellationToken);
            }
        }
        else if (sourceType == "XML" || sourceType == "SOAP")
        {
            var inputXml = XDocument.Parse(messageBody);

            if (integration.DestinationType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                // XML to JSON mapping
                var outputJson = new JObject();
                var engine = mappingEngineFactory.CreateEngine<XDocument, JObject>();

                var globalStatics = await configProvider.GetGlobalStaticValuesAsync(cancellationToken);
                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputXml,
                    outputJson,
                    integration.StaticValues,
                    globalStatics,
                    services,
                    cancellationToken);

                outputPayload = outputJson;
                await ForwardToDestinationAsync(integration, outputJson, null, httpClientFactory, cancellationToken);
            }
            else if (integration.DestinationType.Equals("XML", StringComparison.OrdinalIgnoreCase) ||
                     integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase))
            {
                // XML to XML mapping
                var outputXml = CreateXmlDocument(integration);
                var engine = mappingEngineFactory.CreateEngine<XDocument, XDocument>();

                var globalStatics = await configProvider.GetGlobalStaticValuesAsync(cancellationToken);
                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputXml,
                    outputXml,
                    integration.StaticValues,
                    globalStatics,
                    services,
                    cancellationToken);

                outputPayload = outputXml;
                await ForwardToDestinationAsync(integration, null, outputXml, httpClientFactory, cancellationToken);
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported source type: {sourceType}");
        }

        stopwatch.Stop();

        // Capture output message
        if (messageCaptureProvider != null && integration.EnableMessageCapture)
        {
            await CaptureOutputMessageAsync(
                messageCaptureProvider,
                integration,
                outputPayload,
                correlationId,
                MessageStatus.Success,
                null,
                stopwatch.Elapsed);
        }

        _logger.LogInformation("Message processing completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Determines the integration name from message properties or routing key.
    /// </summary>
    private string? DetermineIntegrationName(BasicDeliverEventArgs eventArgs)
    {
        // First check message headers for IntegrationName
        if (eventArgs.BasicProperties.Headers != null &&
            eventArgs.BasicProperties.Headers.TryGetValue("IntegrationName", out var integrationNameObj))
        {
            if (integrationNameObj is byte[] bytes)
            {
                return Encoding.UTF8.GetString(bytes);
            }
            return integrationNameObj?.ToString();
        }

        // Fall back to routing key if it's not empty
        if (!string.IsNullOrEmpty(eventArgs.RoutingKey))
        {
            return eventArgs.RoutingKey;
        }

        // Use default integration name if configured
        return _defaultIntegrationName;
    }

    /// <summary>
    /// Determines the source type from message content.
    /// </summary>
    private string DetermineSourceType(string messageBody, IntegrationMapping integration)
    {
        // First check if integration has a configured source type
        if (!string.IsNullOrEmpty(integration.SourceType))
        {
            return integration.SourceType;
        }

        // Try to detect from content
        var trimmed = messageBody.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            return "JSON";
        }
        else if (trimmed.StartsWith("<"))
        {
            // Check if it's SOAP by looking for SOAP envelope
            if (trimmed.Contains("soap:Envelope", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("soapenv:Envelope", StringComparison.OrdinalIgnoreCase))
            {
                return "SOAP";
            }
            return "XML";
        }

        throw new InvalidOperationException("Could not determine source type from message content.");
    }

    /// <summary>
    /// Creates an XML document with proper namespace configuration.
    /// </summary>
    private XDocument CreateXmlDocument(IntegrationMapping integration)
    {
        var rootElementName = "root";
        XNamespace rootNamespace = "";

        if (integration.StaticValues?.TryGetValue("TnsNamespace", out var tnsNamespace) == true)
        {
            rootNamespace = tnsNamespace;
        }

        return new XDocument(new XElement(rootNamespace + rootElementName));
    }

    /// <summary>
    /// Forwards the transformed message to the destination.
    /// </summary>
    private async Task ForwardToDestinationAsync(
        IntegrationMapping integration,
        JObject? outputJson,
        XDocument? outputXml,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // Check if output is enabled
        if (!integration.EnableOutput)
        {
            _logger.LogInformation("Output disabled for integration {Integration}, message captured but not forwarded",
                integration.Name);
            return;
        }

        // Use keyed service resolution for direct lookup (eliminates CanHandle iteration)
        var handler = _serviceProvider.GetKeyedService<IDestinationHandler>(integration.DestinationType) ??
                      throw new InvalidOperationException($"No handler found for destination type: {integration.DestinationType}");

        // Create a minimal HTTP context for the handler (handlers expect HttpRequest/HttpResponse)
        // Since we're in a background worker, we'll use the handler's internal logic directly
        // For now, we'll just log that we would forward the message
        _logger.LogInformation("Forwarding {DestinationType} message to {Url}",
            integration.DestinationType, integration.DestinationUrl);

        // Direct HTTP call for RabbitMQ background worker (no HttpContext available)
        using var client = httpClientFactory.CreateClient();

        using var content = outputJson != null
            ? new StringContent(outputJson.ToString(), Encoding.UTF8, "application/json")
            : outputXml != null
                ? new StringContent(outputXml.ToString(), Encoding.UTF8,
                    integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase)
                        ? "text/xml"
                        : "application/xml")
                : throw new InvalidOperationException("No output payload available for forwarding.");

        using var response = await client.PostAsync(integration.DestinationUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Destination returned {response.StatusCode}: {errorBody}");
        }

        _logger.LogInformation("Successfully forwarded message to destination, response: {StatusCode}",
            response.StatusCode);
    }

    /// <summary>
    /// Captures input message.
    /// </summary>
    private async Task CaptureInputMessageAsync(
        IMessageCaptureProvider messageCaptureProvider,
        IntegrationMapping integration,
        string payload,
        string correlationId,
        string sourceType)
    {
        try
        {
            var message = new CapturedMessage
            {
                IntegrationId = Guid.Empty, // Could be extracted from integration config if available
                IntegrationName = integration.Name,
                Direction = MessageDirection.Input,
                Payload = payload,
                Status = MessageStatus.Pending,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["SourceType"] = sourceType,
                    ["Queue"] = _queueName,
                    ["Source"] = "RabbitMQ"
                }
            };

            await messageCaptureProvider.CaptureAsync(message);
            _logger.LogDebug("Captured input message for correlation ID {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture input message");
            // Don't fail the entire operation
        }
    }

    /// <summary>
    /// Captures output message.
    /// </summary>
    private async Task CaptureOutputMessageAsync(
        IMessageCaptureProvider messageCaptureProvider,
        IntegrationMapping integration,
        object? payload,
        string correlationId,
        MessageStatus status,
        string? errorMessage,
        TimeSpan duration)
    {
        try
        {
            var payloadString = payload switch
            {
                JObject json => json.ToString(Formatting.Indented),
                XDocument xml => xml.ToString(),
                string str => str,
                _ => JsonConvert.SerializeObject(payload, Formatting.Indented)
            };

            var message = new CapturedMessage
            {
                IntegrationId = Guid.Empty,
                IntegrationName = integration.Name,
                Direction = MessageDirection.Output,
                Payload = payloadString ?? string.Empty,
                Status = status,
                ErrorMessage = errorMessage,
                Duration = duration,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["DestinationType"] = integration.DestinationType,
                    ["DurationMs"] = duration.TotalMilliseconds.ToString("F2"),
                    ["Queue"] = _queueName,
                    ["Source"] = "RabbitMQ"
                }
            };

            await messageCaptureProvider.CaptureAsync(message);
            _logger.LogDebug("Captured output message for correlation ID {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture output message");
            // Don't fail the entire operation
        }
    }

    /// <summary>
    /// Captures a failed message when no integration context is available.
    /// </summary>
    private async Task CaptureFailedMessageAsync(string correlationId, string errorMessage)
    {
        try
        {
            var messageCaptureProvider = _serviceProvider.GetService<IMessageCaptureProvider>();
            if (messageCaptureProvider == null)
                return;

            var message = new CapturedMessage
            {
                IntegrationId = Guid.Empty,
                IntegrationName = "Unknown",
                Direction = MessageDirection.Input,
                Payload = string.Empty,
                Status = MessageStatus.Failed,
                ErrorMessage = errorMessage,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["Queue"] = _queueName,
                    ["Source"] = "RabbitMQ"
                }
            };

            await messageCaptureProvider.CaptureAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture failed message");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer for queue: {Queue}", _queueName);

        if (_channel != null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection != null)
            await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
