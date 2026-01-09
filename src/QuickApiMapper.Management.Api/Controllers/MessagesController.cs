using Microsoft.AspNetCore.Mvc;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Models;

namespace QuickApiMapper.Management.Api.Controllers;

/// <summary>
/// API controller for querying captured messages.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageCaptureProvider _messageCaptureProvider;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageCaptureProvider messageCaptureProvider,
        ILogger<MessagesController> logger)
    {
        _messageCaptureProvider = messageCaptureProvider ?? throw new ArgumentNullException(nameof(messageCaptureProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Query captured messages with filters and pagination.
    /// </summary>
    /// <param name="integrationId">Filter by integration ID (optional).</param>
    /// <param name="direction">Filter by direction: Input or Output (optional).</param>
    /// <param name="status">Filter by status: Success, Failed, or Pending (optional).</param>
    /// <param name="startDate">Filter by start date (optional).</param>
    /// <param name="endDate">Filter by end date (optional).</param>
    /// <param name="correlationId">Filter by correlation ID (optional).</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of captured messages.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CapturedMessage>>> Query(
        [FromQuery] Guid? integrationId,
        [FromQuery] MessageDirection? direction,
        [FromQuery] MessageStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? correlationId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Limit page size to prevent abuse
        if (pageSize > 100)
            pageSize = 100;

        var filter = new MessageFilter
        {
            IntegrationId = integrationId,
            Direction = direction,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
            CorrelationId = correlationId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        _logger.LogDebug("Querying messages with filter: {Filter}", filter);

        var result = await _messageCaptureProvider.QueryAsync(filter, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific captured message by ID.
    /// </summary>
    /// <param name="messageId">Message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Captured message if found.</returns>
    [HttpGet("{messageId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapturedMessage>> GetById(
        string messageId,
        CancellationToken cancellationToken)
    {
        var message = await _messageCaptureProvider.GetByIdAsync(messageId, cancellationToken);

        if (message == null)
        {
            _logger.LogWarning("Message {MessageId} not found", messageId);
            return NotFound(new { Message = $"Message {messageId} not found" });
        }

        return Ok(message);
    }

    /// <summary>
    /// Get statistics for captured messages.
    /// </summary>
    /// <param name="integrationId">Integration ID to get statistics for.</param>
    /// <param name="startDate">Start date for statistics (optional).</param>
    /// <param name="endDate">End date for statistics (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Message statistics.</returns>
    [HttpGet("statistics/{integrationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageStatistics>> GetStatistics(
        Guid integrationId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting statistics for integration {IntegrationId}", integrationId);

        var statistics = await _messageCaptureProvider.GetStatisticsAsync(
            integrationId,
            startDate,
            endDate,
            cancellationToken);

        return Ok(statistics);
    }

    /// <summary>
    /// Delete old messages based on retention policy.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain messages (default: 7).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages deleted.</returns>
    [HttpDelete("purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> PurgeOldMessages(
        [FromQuery] int retentionDays = 7,
        CancellationToken cancellationToken = default)
    {
        if (retentionDays < 1)
        {
            return BadRequest(new { Message = "Retention days must be at least 1" });
        }

        _logger.LogInformation("Purging messages older than {RetentionDays} days", retentionDays);

        var retentionPeriod = TimeSpan.FromDays(retentionDays);
        var deletedCount = await _messageCaptureProvider.PurgeOldMessagesAsync(retentionPeriod, cancellationToken);

        _logger.LogInformation("Purged {DeletedCount} messages", deletedCount);

        return Ok(new { DeletedCount = deletedCount });
    }
}
