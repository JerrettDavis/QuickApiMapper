using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Behaviors;

/// <summary>
/// Configuration for OAuth2/OIDC authentication behavior.
/// </summary>
public sealed class AuthenticationConfig
{
    public required string TokenEndpoint { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Scope { get; init; }
    public string GrantType { get; init; } = "client_credentials";
    public TimeSpan TokenCacheExpiry { get; init; } = TimeSpan.FromMinutes(50); // Slightly less than typical 1h expiry
}

/// <summary>
/// Cached token information.
/// </summary>
public sealed class TokenInfo
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public string? TokenType { get; init; } = "Bearer";
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}

/// <summary>
/// Authentication behavior that handles OAuth2/OIDC token acquisition and injection.
/// Implements PreRun behavior to acquire tokens before HTTP requests are made.
/// </summary>
public class AuthenticationBehavior(
    AuthenticationConfig config,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthenticationBehavior> logger
) : IPreRunBehavior
{

    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    
    private TokenInfo? _cachedToken;

    public string Name => "Authentication";
    public int Order => 100; // Execute early in PreRun pipeline

    public async Task ExecuteAsync(MappingContext context)
    {
        logger.LogDebug("Starting authentication behavior");

        try
        {
            var token = await GetValidTokenAsync(context.CancellationToken);
            
            // Store the token in context properties for use by HTTP clients
            context.Properties["AuthToken"] = token;
            
            // If an HttpClient is available in the service provider, configure it
            if (context.ServiceProvider.GetService<HttpClient>() is { } httpClient)
                ConfigureHttpClient(httpClient, token);
            
            logger.LogDebug("Authentication behavior completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authentication behavior failed");
            throw;
        }
    }

    /// <summary>
    /// Gets a valid access token, using cached token if available and not expired.
    /// </summary>
    private async Task<TokenInfo> GetValidTokenAsync(CancellationToken cancellationToken)
    {
        // Check if we have a valid cached token
        if (_cachedToken != null && !_cachedToken.IsExpired)
        {
            logger.LogDebug("Using cached authentication token");
            return _cachedToken;
        }

        // Acquire lock to prevent concurrent token requests
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken != null && !_cachedToken.IsExpired)
            {
                return _cachedToken;
            }

            logger.LogDebug("Acquiring new authentication token from {TokenEndpoint}", config.TokenEndpoint);
            
            // Acquire new token
            _cachedToken = await AcquireTokenAsync(cancellationToken);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Acquires a new access token from the OAuth2/OIDC endpoint.
    /// </summary>
    private async Task<TokenInfo> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient();
        
        // Prepare token request
        var tokenRequest = new List<KeyValuePair<string, string>>
        {
            new("grant_type", config.GrantType),
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret)
        };

        if (!string.IsNullOrEmpty(config.Scope))
        {
            tokenRequest.Add(new("scope", config.Scope));
        }

        using var requestContent = new FormUrlEncodedContent(tokenRequest);
        using var response = await httpClient.PostAsync(config.TokenEndpoint, requestContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Token acquisition failed with status {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Extract token information
        var accessToken = tokenResponse.GetProperty("access_token").GetString()
                          ?? throw new InvalidOperationException("Access token not found in response");

        var tokenType = tokenResponse.TryGetProperty("token_type", out var tokenTypeElement)
            ? tokenTypeElement.GetString()
            : "Bearer";

        // Calculate expiry time
        var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expiresInElement)
            ? expiresInElement.GetInt32()
            : (int)config.TokenCacheExpiry.TotalSeconds;

        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 60 second buffer

        logger.LogDebug("Successfully acquired authentication token, expires at {ExpiresAt}", expiresAt);

        return new TokenInfo
        {
            AccessToken = accessToken,
            TokenType = tokenType,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Configures the HttpClient with the authentication token.
    /// </summary>
    private void ConfigureHttpClient(HttpClient httpClient, TokenInfo token)
    {
        // Clear the existing authorization header
        httpClient.DefaultRequestHeaders.Authorization = null;
        
        // Set a new authorization header
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType ?? "Bearer", 
            token.AccessToken);
        
        logger.LogDebug("HttpClient configured with {TokenType} authentication", token.TokenType);
    }

}