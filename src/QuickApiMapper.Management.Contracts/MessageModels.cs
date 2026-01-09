namespace QuickApiMapper.Management.Contracts.Models;

/// <summary>
/// DTO for captured message.
/// </summary>
public class CapturedMessageDto
{
    public string Id { get; set; } = string.Empty;
    public Guid IntegrationId { get; set; }
    public string? IntegrationName { get; set; }
    public string Direction { get; set; } = string.Empty; // "Input" or "Output"
    public string Payload { get; set; } = string.Empty;
    public bool IsTruncated { get; set; }
    public string Status { get; set; } = string.Empty; // "Success", "Failed", "Pending"
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Paged result for messages.
/// </summary>
public class MessagePagedResult
{
    public List<CapturedMessageDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Message statistics DTO.
/// </summary>
public class MessageStatisticsDto
{
    public int TotalMessages { get; set; }
    public int SuccessfulMessages { get; set; }
    public int FailedMessages { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan? AverageDuration { get; set; }
}
