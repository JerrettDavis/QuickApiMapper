using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuickApiMapper.Contracts;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Models;

namespace QuickApiMapper.Behaviors;

/// <summary>
/// Message capture behavior that captures input and output messages during mapping operations.
/// Implements WholeRun behavior to wrap the entire mapping process.
/// </summary>
public sealed class MessageCaptureBehavior : IWholeRunBehavior
{
    private readonly IMessageCaptureProvider _messageCaptureProvider;
    private readonly ILogger<MessageCaptureBehavior> _logger;

    public MessageCaptureBehavior(
        IMessageCaptureProvider messageCaptureProvider,
        ILogger<MessageCaptureBehavior> logger)
    {
        _messageCaptureProvider = messageCaptureProvider ?? throw new ArgumentNullException(nameof(messageCaptureProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "MessageCapture";
    public int Order => 5; // Execute early to capture all messages

    public async Task<MappingResult> ExecuteAsync(MappingContext context, Func<MappingContext, Task<MappingResult>> next)
    {
        // Skip message capture if not enabled for this integration
        if (!ShouldCaptureMessages(context))
        {
            _logger.LogDebug("Message capture disabled for this integration, skipping");
            return await next(context);
        }

        var correlationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Capture input message
        try
        {
            var inputPayload = context.Source != null ? SerializePayload(context.Source) : string.Empty;
            var inputMessage = new CapturedMessage
            {
                IntegrationId = GetIntegrationId(context),
                IntegrationName = GetIntegrationName(context),
                Direction = MessageDirection.Input,
                Payload = inputPayload,
                Status = MessageStatus.Pending,
                CorrelationId = correlationId,
                Timestamp = startTime,
                Metadata = new Dictionary<string, string>
                {
                    ["SourceType"] = GetSourceType(context),
                    ["MappingCount"] = context.Mappings.Count().ToString()
                }
            };

            await _messageCaptureProvider.CaptureAsync(inputMessage);
            _logger.LogDebug("Captured input message with correlation ID {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture input message");
            // Don't fail the entire operation if message capture fails
        }

        // Execute the mapping pipeline
        MappingResult result;
        try
        {
            result = await next(context);
            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Capture failed output message
            await CaptureOutputMessage(
                context,
                correlationId,
                null,
                MessageStatus.Failed,
                ex.Message,
                stopwatch.Elapsed);

            throw;
        }

        // Capture output message
        await CaptureOutputMessage(
            context,
            correlationId,
            context.Destination,
            result.IsSuccess ? MessageStatus.Success : MessageStatus.Failed,
            result.IsSuccess ? null : result.ErrorMessage,
            stopwatch.Elapsed);

        return result;
    }

    private async Task CaptureOutputMessage(
        MappingContext context,
        string correlationId,
        object? destination,
        MessageStatus status,
        string? errorMessage,
        TimeSpan duration)
    {
        try
        {
            var outputPayload = destination != null ? SerializePayload(destination) : null;
            var outputMessage = new CapturedMessage
            {
                IntegrationId = GetIntegrationId(context),
                IntegrationName = GetIntegrationName(context),
                Direction = MessageDirection.Output,
                Payload = outputPayload ?? string.Empty,
                Status = status,
                ErrorMessage = errorMessage,
                Duration = duration,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>
                {
                    ["DestinationType"] = GetDestinationType(context),
                    ["DurationMs"] = duration.TotalMilliseconds.ToString("F2")
                }
            };

            await _messageCaptureProvider.CaptureAsync(outputMessage);
            _logger.LogDebug("Captured output message with correlation ID {CorrelationId}, status {Status}",
                correlationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture output message");
            // Don't fail the entire operation if message capture fails
        }
    }

    private static string SerializePayload(object payload)
    {
        if (payload is string str)
            return str;

        return JsonConvert.SerializeObject(payload, Formatting.Indented);
    }

    private static bool ShouldCaptureMessages(MappingContext context)
    {
        // Check if EnableMessageCapture property is set in context properties
        if (context.Properties.TryGetValue("EnableMessageCapture", out var enableCapture))
        {
            return enableCapture is bool enabled && enabled;
        }

        // Default to true if not specified
        return true;
    }

    private static Guid GetIntegrationId(MappingContext context)
    {
        if (context.Properties.TryGetValue("IntegrationId", out var id) && id is Guid guid)
            return guid;

        return Guid.Empty;
    }

    private static string GetIntegrationName(MappingContext context)
    {
        if (context.Properties.TryGetValue("IntegrationName", out var name) && name is string str)
            return str;

        return "Unknown";
    }

    private static string GetSourceType(MappingContext context)
    {
        if (context.Properties.TryGetValue("SourceType", out var type) && type is string str)
            return str;

        return "Unknown";
    }

    private static string GetDestinationType(MappingContext context)
    {
        if (context.Properties.TryGetValue("DestinationType", out var type) && type is string str)
            return str;

        return "Unknown";
    }
}
