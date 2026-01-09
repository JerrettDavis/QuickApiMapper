using System.Collections.Concurrent;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Models;
using QuickApiMapper.MessageCapture.Abstractions.Options;

namespace QuickApiMapper.MessageCapture.InMemory.Providers;

/// <summary>
/// In-memory implementation of message capture provider.
/// Suitable for development and testing. Messages are lost on restart.
/// </summary>
public class InMemoryMessageCaptureProvider : IMessageCaptureProvider
{
    private readonly ConcurrentDictionary<string, CapturedMessage> _messages = new();
    private readonly MessageCaptureOptions _options;

    public InMemoryMessageCaptureProvider(MessageCaptureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task CaptureAsync(CapturedMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        // Truncate payload if too large
        var maxSizeBytes = _options.MaxPayloadSizeKB * 1024;
        if (message.Payload.Length > maxSizeBytes)
        {
            message.Payload = message.Payload.Substring(0, maxSizeBytes);
            message.IsTruncated = true;
        }

        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<CapturedMessage?> GetByIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    public Task<PagedResult<CapturedMessage>> QueryAsync(MessageFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _messages.Values.AsEnumerable();

        // Apply filters
        if (filter.IntegrationId.HasValue)
            query = query.Where(m => m.IntegrationId == filter.IntegrationId.Value);

        if (filter.Direction.HasValue)
            query = query.Where(m => m.Direction == filter.Direction.Value);

        if (filter.Status.HasValue)
            query = query.Where(m => m.Status == filter.Status.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(m => m.Timestamp >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(m => m.Timestamp <= filter.EndDate.Value);

        if (!string.IsNullOrEmpty(filter.CorrelationId))
            query = query.Where(m => m.CorrelationId == filter.CorrelationId);

        // Order by timestamp descending (newest first)
        query = query.OrderByDescending(m => m.Timestamp);

        var totalCount = query.Count();

        // Apply pagination
        var items = query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<CapturedMessage>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        });
    }

    public Task<int> PurgeOldMessagesAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        var oldMessages = _messages.Where(kvp => kvp.Value.Timestamp < cutoffDate).ToList();

        var removedCount = 0;
        foreach (var kvp in oldMessages)
        {
            if (_messages.TryRemove(kvp.Key, out _))
                removedCount++;
        }

        return Task.FromResult(removedCount);
    }

    public Task<MessageStatistics> GetStatisticsAsync(
        Guid integrationId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _messages.Values.Where(m => m.IntegrationId == integrationId);

        if (startDate.HasValue)
            query = query.Where(m => m.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.Timestamp <= endDate.Value);

        var messages = query.ToList();
        var successful = messages.Count(m => m.Status == MessageStatus.Success);
        var failed = messages.Count(m => m.Status == MessageStatus.Failed);

        var durationsWithValues = messages.Where(m => m.Duration.HasValue).Select(m => m.Duration!.Value).ToList();
        var avgDuration = durationsWithValues.Any()
            ? TimeSpan.FromTicks((long)durationsWithValues.Average(d => d.Ticks))
            : (TimeSpan?)null;

        return Task.FromResult(new MessageStatistics
        {
            TotalMessages = messages.Count,
            SuccessfulMessages = successful,
            FailedMessages = failed,
            AverageDuration = avgDuration
        });
    }
}
