using Microsoft.Extensions.Logging;

namespace QuickApiMapper.Management.Api.Services;

/// <summary>
/// Source-generated logging methods for IntegrationService.
/// Uses compile-time code generation for high-performance logging.
/// </summary>
public partial class IntegrationService
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Created integration {IntegrationId} with name {Name}")]
    partial void LogIntegrationCreated(Guid integrationId, string name);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Updated integration {IntegrationId} to version {Version}")]
    partial void LogIntegrationUpdated(Guid integrationId, int version);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Deleted integration {IntegrationId} with name {Name}")]
    partial void LogIntegrationDeleted(Guid integrationId, string name);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Integration {IntegrationId} not found")]
    partial void LogIntegrationNotFound(Guid integrationId);
}
