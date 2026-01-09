using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Behaviors;

/// <summary>
/// Configuration for HTTP client behavior.
/// </summary>
public sealed class HttpClientConfiguration
{
    public Dictionary<string, string> DefaultHeaders { get; init; } = new();
    public TimeSpan? Timeout { get; init; }
    public string? UserAgent { get; init; }
    public bool FollowRedirects { get; init; } = true;
}

/// <summary>
/// Behavior that configures HTTP clients with custom headers, timeouts, and other settings.
/// This works in conjunction with the AuthenticationBehavior to provide complete HTTP configuration.
/// </summary>
public sealed class HttpClientConfigurationBehavior(
    HttpClientConfiguration config,
    ILogger<HttpClientConfigurationBehavior> logger
) : IPreRunBehavior
{
    public string Name => "HttpClientConfiguration";
    public int Order => 200; // Execute after authentication (100) but before other behaviors

    public Task ExecuteAsync(MappingContext context)
    {
        logger.LogDebug("Starting HTTP client configuration behavior");

        try
        {
            // Configure any HttpClient instances available in the service provider
            var httpClients = context.ServiceProvider.GetServices<HttpClient>();

            foreach (var httpClient in httpClients)
            {
                ConfigureHttpClient(httpClient);
            }

            // Also configure the IHttpClientFactory if available
            if (context.ServiceProvider.GetService<IHttpClientFactory>() is { } factory)
            {
                // Store configuration in context for use by other behaviors or resolvers
                context.Properties["HttpClientFactory"] = factory;
                context.Properties["HttpClientConfig"] = config;
            }

            logger.LogDebug("HTTP client configuration behavior completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP client configuration behavior failed");
            throw;
        }

        return Task.CompletedTask;
    }

    private void ConfigureHttpClient(HttpClient httpClient)
    {
        // Set timeout if specified
        if (config.Timeout.HasValue)
        {
            httpClient.Timeout = config.Timeout.Value;
            logger.LogDebug("Set HTTP client timeout to {Timeout}", config.Timeout.Value);
        }

        // Set user agent if specified
        if (!string.IsNullOrEmpty(config.UserAgent))
        {
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);
            logger.LogDebug("Set HTTP client user agent to {UserAgent}", config.UserAgent);
        }

        // Add default headers
        foreach (var header in config.DefaultHeaders)
        {
            // Remove existing header if present
            httpClient.DefaultRequestHeaders.Remove(header.Key);

            // Add new header
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            logger.LogDebug("Added HTTP client header: {HeaderName} = {HeaderValue}",
                header.Key, header.Value);
        }

        logger.LogDebug("HTTP client configured with {HeaderCount} default headers",
            config.DefaultHeaders.Count);
    }
}