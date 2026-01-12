using System.ComponentModel.DataAnnotations;

namespace QuickApiMapper.MessageCapture.Abstractions.Options;

/// <summary>
/// Configuration options for message capture.
/// </summary>
public class MessageCaptureOptions
{
    /// <summary>
    /// Gets or sets the maximum payload size in kilobytes before truncation.
    /// </summary>
    [Range(1, 10240, ErrorMessage = "MaxPayloadSizeKB must be between 1 KB and 10 MB")]
    public int MaxPayloadSizeKB { get; set; } = 1024; // 1MB default

    /// <summary>
    /// Gets or sets the retention period for captured messages.
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "365.00:00:00", ErrorMessage = "RetentionPeriod must be between 1 minute and 365 days")]
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets a value indicating whether payload compression is enabled.
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of sensitive header names that should be redacted.
    /// </summary>
    [Required(ErrorMessage = "SensitiveHeaders cannot be null")]
    [MinLength(1, ErrorMessage = "At least one sensitive header must be configured")]
    public List<string> SensitiveHeaders { get; set; } =
    [
        "Authorization",
        "X-API-Key",
        "Cookie",
        "Set-Cookie"
    ];
}
