namespace QuickApiMapper.MessageCapture.Abstractions.Models;

/// <summary>
/// Represents a captured message (request or response).
/// </summary>
public class CapturedMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the captured message.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the ID of the integration that processed this message.
    /// </summary>
    public Guid IntegrationId { get; set; }

    /// <summary>
    /// Gets or sets the name of the integration.
    /// </summary>
    public string? IntegrationName { get; set; }

    /// <summary>
    /// Gets or sets the direction of the message (Input or Output).
    /// </summary>
    public MessageDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the message payload content.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the payload was truncated due to size limits.
    /// </summary>
    public bool IsTruncated { get; set; }

    /// <summary>
    /// Gets or sets the processing status of the message.
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the processing duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking related messages.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was captured.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata associated with the message.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Direction of the message flow.
/// </summary>
public enum MessageDirection
{
    /// <summary>
    /// Incoming message to the integration.
    /// </summary>
    Input,

    /// <summary>
    /// Outgoing message from the integration.
    /// </summary>
    Output
}

/// <summary>
/// Status of message processing.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message was processed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Message processing failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Message is pending processing.
    /// </summary>
    Pending
}
