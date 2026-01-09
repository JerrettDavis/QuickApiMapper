using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Destinations;

/// <summary>
/// Handles JSON destination requests by forwarding JSON payloads to external APIs.
/// Provides reliable HTTP communication with proper error handling and logging.
/// </summary>
public sealed class JsonDestinationHandler : IDestinationHandler
{
    private readonly ILogger<JsonDestinationHandler>? _logger;

    /// <summary>
    /// Initializes a new instance of the JsonDestinationHandler class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public JsonDestinationHandler(ILogger<JsonDestinationHandler>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines if this handler can process the specified destination type.
    /// </summary>
    /// <param name="destinationType">The destination type to check.</param>
    /// <returns>True if the destination type is "JSON" (case-insensitive).</returns>
    public bool CanHandle(string destinationType) =>
        destinationType.Equals("JSON", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Handles a JSON destination request by forwarding the JSON payload to the target URL.
    /// </summary>
    /// <param name="integration">The integration mapping configuration.</param>
    /// <param name="outJson">The JSON payload to send.</param>
    /// <param name="outXml">The XML payload (not used by this handler).</param>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="resp">The HTTP response to write to.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public async Task HandleAsync(
        IntegrationMapping integration,
        JObject? outJson,
        XDocument? outXml,
        HttpRequest req,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        if (integration == null)
        {
            throw new ArgumentNullException(nameof(integration));
        }

        if (httpClientFactory == null)
        {
            throw new ArgumentNullException(nameof(httpClientFactory));
        }

        if (resp == null)
        {
            throw new ArgumentNullException(nameof(resp));
        }

        // Validate that we have JSON output
        if (outJson == null)
        {
            _logger?.LogWarning("No JSON output provided for integration {Integration}", integration.Name);
            resp.StatusCode = 400;
            await resp.WriteAsync("No JSON output could be generated.", cancellationToken);
            return;
        }

        try
        {
            _logger?.LogInformation("Sending JSON request to {Url} for integration {Integration}", 
                integration.DestinationUrl, integration.Name);

            // Create HTTP client and prepare request
            using var client = httpClientFactory.CreateClient();
            var jsonContent = outJson.ToString();
            var outgoingContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, integration.DestinationUrl);
            
            httpRequest.Content = outgoingContent;

            _logger?.LogDebug("Outgoing JSON payload: {Json}", jsonContent);

            // Send the request
            using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);

            // Forward the response
            resp.StatusCode = (int)httpResponse.StatusCode;
            resp.ContentType = httpResponse.Content.Headers.ContentType?.ToString() ?? "application/json";
            
            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            await resp.WriteAsync(responseBody, cancellationToken);

            _logger?.LogInformation("JSON request completed with status {StatusCode} for integration {Integration}", 
                httpResponse.StatusCode, integration.Name);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP request failed for integration {Integration} to {Url}", 
                integration.Name, integration.DestinationUrl);
            resp.StatusCode = 502; // Bad Gateway
            await resp.WriteAsync($"Failed to communicate with destination service: {ex.Message}", cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogError(ex, "Request timeout for integration {Integration} to {Url}", 
                integration.Name, integration.DestinationUrl);
            resp.StatusCode = 504; // Gateway Timeout
            await resp.WriteAsync("Request to destination service timed out.", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error handling JSON destination for integration {Integration}", 
                integration.Name);
            resp.StatusCode = 500;
            await resp.WriteAsync($"Internal server error: {ex.Message}", cancellationToken);
        }
    }
}
