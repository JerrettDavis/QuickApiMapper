using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;
using System.Buffers;

namespace QuickApiMapper.Application.Services;

/// <summary>
/// High-performance message processor demonstrating multiple C# 14 and .NET 10 features.
/// Combines extension members, field keyword, implicit Span conversions, and source-generated logging.
/// </summary>
public partial class HighPerformanceMessageProcessor
{
    private readonly ILogger<HighPerformanceMessageProcessor> _logger;

    // C# 14: Field keyword for custom property logic
    public int ProcessedMessageCount
    {
        get => field;
        private set
        {
            field = value;
            if (value % 1000 == 0)
                LogProcessingMilestone(value);
        }
    }

    // C# 14: Field keyword with validation
    public int MaxBatchSize
    {
        get => field;
        set
        {
            if (value <= 0 || value > 10000)
                throw new ArgumentOutOfRangeException(nameof(value), "Must be between 1 and 10000");
            field = value;
        }
    }

    public HighPerformanceMessageProcessor(ILogger<HighPerformanceMessageProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MaxBatchSize = 100; // Default batch size
    }

    /// <summary>
    /// Processes a batch of integrations using C# 14 extension members.
    /// </summary>
    public async Task<ProcessingResult> ProcessBatchAsync(
        IEnumerable<IntegrationMapping> integrations,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var processedCount = 0;
        var errors = new List<string>();

        foreach (var integration in integrations)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // C# 14: Extension members in action
                if (!integration.IsFullyEnabled())
                {
                    LogIntegrationSkipped(integration.Name, "Not fully enabled");
                    continue;
                }

                if (integration.IsSoapIntegration() && !integration.HasFieldMappings())
                {
                    LogIntegrationSkipped(integration.Name, "SOAP integration missing field mappings");
                    errors.Add($"{integration.Name}: Missing field mappings");
                    continue;
                }

                // Process the integration
                await ProcessIntegrationAsync(integration, cancellationToken);

                processedCount++;
                ProcessedMessageCount++; // Triggers field keyword logic

                LogIntegrationProcessed(integration.Name, integration.ProtocolFlow());
            }
            catch (Exception ex)
            {
                LogIntegrationError(integration.Name, ex.Message);
                errors.Add($"{integration.Name}: {ex.Message}");
            }
        }

        var duration = DateTime.UtcNow - startTime;

        LogBatchCompleted(processedCount, errors.Count, duration.TotalSeconds);

        return new ProcessingResult
        {
            ProcessedCount = processedCount,
            ErrorCount = errors.Count,
            Errors = errors,
            Duration = duration
        };
    }

    /// <summary>
    /// Efficiently parses message headers using C# 14 implicit Span conversions.
    /// </summary>
    public Dictionary<string, string> ParseMessageHeaders(string headerString)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(headerString))
            return headers;

        // C# 14: Implicit Span conversion for zero-allocation parsing
        ReadOnlySpan<char> remaining = headerString.AsSpan();

        while (!remaining.IsEmpty)
        {
            int lineEnd = remaining.IndexOf('\n');
            if (lineEnd == -1)
                lineEnd = remaining.Length;

            // C# 14: Span slicing
            ReadOnlySpan<char> line = remaining[..lineEnd].Trim();

            if (!line.IsEmpty)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    // C# 14: Zero-allocation string operations
                    var key = line[..colonIndex].Trim().ToString();
                    var value = line[(colonIndex + 1)..].Trim().ToString();
                    headers[key] = value;
                }
            }

            // C# 14: Advance span
            remaining = lineEnd < remaining.Length ? remaining[(lineEnd + 1)..] : ReadOnlySpan<char>.Empty;
        }

        LogHeadersParsed(headers.Count);

        return headers;
    }

    /// <summary>
    /// Validates configuration using C# 14 nameof with unbound generics.
    /// </summary>
    public void ValidateConfiguration<TConfig>(TConfig config) where TConfig : class
    {
        if (config == null)
        {
            // C# 14: nameof with unbound generic type
            throw new ArgumentNullException(
                nameof(config),
                $"Configuration of type {nameof(TConfig)} cannot be null. " +
                $"Expected types include {nameof(List<>)}, {nameof(Dictionary<,>)}, etc.");
        }

        LogConfigurationValidated(typeof(TConfig).Name);
    }

    /// <summary>
    /// Processes a buffer efficiently using C# 14 implicit Span conversions.
    /// </summary>
    public async Task<int> ProcessBufferAsync(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        // C# 14: Implicit Span conversion from array slice
        Memory<byte> segment = buffer.AsMemory()[offset..(offset + count)];

        // Process segment efficiently
        await Task.Delay(1); // Simulate async work

        LogBufferProcessed(count);

        return count;
    }

    private async Task ProcessIntegrationAsync(IntegrationMapping integration, CancellationToken cancellationToken)
    {
        // Simulate processing
        await Task.Delay(10, cancellationToken);

        // C# 14: Extension members provide clean validation
        if (integration.TotalTransformerCount() > 50)
        {
            LogHighTransformerCount(integration.Name, integration.TotalTransformerCount());
        }
    }

    // C# 14: Source-generated logging methods
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Integration '{IntegrationName}' processed: {ProtocolFlow}")]
    partial void LogIntegrationProcessed(string integrationName, string protocolFlow);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "Integration '{IntegrationName}' skipped: {Reason}")]
    partial void LogIntegrationSkipped(string integrationName, string reason);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Error,
        Message = "Integration '{IntegrationName}' error: {ErrorMessage}")]
    partial void LogIntegrationError(string integrationName, string errorMessage);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Information,
        Message = "Batch completed: {ProcessedCount} processed, {ErrorCount} errors in {DurationSeconds:F2}s")]
    partial void LogBatchCompleted(int processedCount, int errorCount, double durationSeconds);

    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Debug,
        Message = "Parsed {HeaderCount} message headers")]
    partial void LogHeadersParsed(int headerCount);

    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Debug,
        Message = "Configuration validated for type {TypeName}")]
    partial void LogConfigurationValidated(string typeName);

    [LoggerMessage(
        EventId = 5007,
        Level = LogLevel.Debug,
        Message = "Processed buffer: {ByteCount} bytes")]
    partial void LogBufferProcessed(int byteCount);

    [LoggerMessage(
        EventId = 5008,
        Level = LogLevel.Information,
        Message = "Processing milestone reached: {MessageCount} messages processed")]
    partial void LogProcessingMilestone(int messageCount);

    [LoggerMessage(
        EventId = 5009,
        Level = LogLevel.Warning,
        Message = "Integration '{IntegrationName}' has {TransformerCount} transformers (consider optimization)")]
    partial void LogHighTransformerCount(string integrationName, int transformerCount);
}

/// <summary>
/// Result of batch processing operation.
/// </summary>
public class ProcessingResult
{
    public int ProcessedCount { get; init; }
    public int ErrorCount { get; init; }
    public List<string> Errors { get; init; } = [];
    public TimeSpan Duration { get; init; }

    // C# 14: Computed property using modern syntax
    public double MessagesPerSecond =>
        Duration.TotalSeconds > 0 ? ProcessedCount / Duration.TotalSeconds : 0;

    public bool IsSuccessful => ErrorCount == 0;
}
