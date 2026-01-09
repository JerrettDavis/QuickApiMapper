namespace QuickApiMapper.MessageCapture.Abstractions.Options;

/// <summary>
/// Configuration options for message capture.
/// </summary>
public class MessageCaptureOptions
{
    /// <summary>
    /// Gets or sets the maximum payload size in kilobytes before truncation.
    /// </summary>
    public int MaxPayloadSizeKB { get; set; } = 1024; // 1MB default

    /// <summary>
    /// Gets or sets the retention period for captured messages.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets a value indicating whether payload compression is enabled.
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of sensitive header names that should be redacted.
    /// </summary>
    public List<string> SensitiveHeaders { get; set; } = new()
    {
        "Authorization",
        "X-API-Key",
        "Cookie",
        "Set-Cookie"
    };
}
