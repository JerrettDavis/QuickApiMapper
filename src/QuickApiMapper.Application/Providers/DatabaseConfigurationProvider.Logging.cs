using Microsoft.Extensions.Logging;

namespace QuickApiMapper.Application.Providers;

/// <summary>
/// Source-generated logging methods for DatabaseConfigurationProvider.
/// Uses compile-time code generation for high-performance logging.
/// </summary>
public partial class DatabaseConfigurationProvider
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Loaded {Count} active integrations from database")]
    partial void LogIntegrationsLoaded(int count);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Found integration with ID '{Id}' in database")]
    partial void LogIntegrationFoundById(string id);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Integration with ID '{Id}' not found in database")]
    partial void LogIntegrationNotFoundById(string id);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "Found integration '{Name}' in database")]
    partial void LogIntegrationFoundByName(string name);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Warning,
        Message = "Integration '{Name}' not found in database")]
    partial void LogIntegrationNotFoundByName(string name);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Debug,
        Message = "Found integration with endpoint '{Endpoint}' in database")]
    partial void LogIntegrationFoundByEndpoint(string endpoint);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Warning,
        Message = "Integration with endpoint '{Endpoint}' not found in database")]
    partial void LogIntegrationNotFoundByEndpoint(string endpoint);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Debug,
        Message = "Loaded {Count} global static values from database")]
    partial void LogGlobalStaticValuesLoaded(int count);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Warning,
        Message = "Invalid GUID format for integration ID: {Id}")]
    partial void LogInvalidGuidFormat(string id);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Debug,
        Message = "Namespaces not yet implemented in database provider, returning empty dictionary")]
    partial void LogNamespacesNotImplemented();
}
