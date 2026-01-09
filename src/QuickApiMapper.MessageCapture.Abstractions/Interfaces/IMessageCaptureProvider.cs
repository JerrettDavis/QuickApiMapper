using QuickApiMapper.MessageCapture.Abstractions.Models;

namespace QuickApiMapper.MessageCapture.Abstractions.Interfaces;

/// <summary>
/// Provider for capturing and storing messages.
/// </summary>
public interface IMessageCaptureProvider
{
    /// <summary>
    /// Captures a message.
    /// </summary>
    Task CaptureAsync(CapturedMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific message by ID.
    /// </summary>
    Task<CapturedMessage?> GetByIdAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries messages with filtering and pagination.
    /// </summary>
    Task<PagedResult<CapturedMessage>> QueryAsync(MessageFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes messages older than the retention period.
    /// </summary>
    Task<int> PurgeOldMessagesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets message statistics for an integration.
    /// </summary>
    Task<MessageStatistics> GetStatisticsAsync(Guid integrationId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about captured messages.
/// </summary>
public class MessageStatistics
{
    /// <summary>
    /// Gets or sets the total number of messages.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of successfully processed messages.
    /// </summary>
    public int SuccessfulMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of failed messages.
    /// </summary>
    public int FailedMessages { get; set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalMessages > 0 ? (double)SuccessfulMessages / TotalMessages * 100 : 0;

    /// <summary>
    /// Gets or sets the average processing duration.
    /// </summary>
    public TimeSpan? AverageDuration { get; set; }
}
