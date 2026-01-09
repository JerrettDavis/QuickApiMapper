using System.Net.Http.Json;
using QuickApiMapper.Management.Api.Models;

namespace QuickApiMapper.Designer.Web.Services;

/// <summary>
/// HTTP client for communicating with the Management API.
/// </summary>
public class IntegrationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IntegrationApiClient> _logger;

    public IntegrationApiClient(HttpClient httpClient, ILogger<IntegrationApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Integration endpoints

    public async Task<List<IntegrationDto>> GetAllIntegrationsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching integrations from {BaseUrl}/api/integrations", _httpClient.BaseAddress);
            var integrations = await _httpClient.GetFromJsonAsync<List<IntegrationDto>>("api/integrations");
            _logger.LogInformation("Successfully fetched {Count} integrations", integrations?.Count ?? 0);
            return integrations ?? new List<IntegrationDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching integrations from {Url}: {Message}",
                $"{_httpClient.BaseAddress}api/integrations", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching integrations: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<IntegrationDto?> GetIntegrationByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IntegrationDto>($"api/integrations/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching integration {IntegrationId}", id);
            return null;
        }
    }

    public async Task<IntegrationDto?> CreateIntegrationAsync(CreateIntegrationRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/integrations", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IntegrationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating integration");
            return null;
        }
    }

    public async Task<IntegrationDto?> UpdateIntegrationAsync(Guid id, UpdateIntegrationRequest request)
    {
        try
        {
            using var response = await _httpClient.PutAsJsonAsync($"api/integrations/{id}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IntegrationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integration {IntegrationId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteIntegrationAsync(Guid id)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync($"api/integrations/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting integration {IntegrationId}", id);
            return false;
        }
    }

    public async Task<TestMappingResponse?> TestIntegrationAsync(Guid id, TestMappingRequest request)
    {
        try
        {
            _logger.LogInformation("Testing integration {IntegrationId}", id);
            using var response = await _httpClient.PostAsJsonAsync($"api/integrations/{id}/test", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TestMappingResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing integration {IntegrationId}", id);
            return null;
        }
    }

    // Schema endpoints

    public async Task<SchemaImportResponse?> ImportJsonSchemaAsync(ImportJsonSchemaRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/schemas/json/import", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SchemaImportResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing JSON schema");
            return null;
        }
    }

    public async Task<SchemaImportResponse?> ImportWsdlAsync(ImportWsdlRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/schemas/wsdl/import", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SchemaImportResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing WSDL");
            return null;
        }
    }

    // Transformer/Behavior metadata endpoints

    public async Task<List<TransformerMetadata>> GetTransformersAsync()
    {
        try
        {
            _logger.LogInformation("Fetching transformers from {BaseUrl}/api/transformers", _httpClient.BaseAddress);
            var transformers = await _httpClient.GetFromJsonAsync<List<TransformerMetadata>>("api/transformers");
            _logger.LogInformation("Successfully fetched {Count} transformers", transformers?.Count ?? 0);
            return transformers ?? new List<TransformerMetadata>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching transformers from {Url}: {Message}",
                $"{_httpClient.BaseAddress}api/transformers", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transformers: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<BehaviorMetadata>> GetBehaviorsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching behaviors from {BaseUrl}/api/behaviors", _httpClient.BaseAddress);
            var behaviors = await _httpClient.GetFromJsonAsync<List<BehaviorMetadata>>("api/behaviors");
            _logger.LogInformation("Successfully fetched {Count} behaviors", behaviors?.Count ?? 0);
            return behaviors ?? new List<BehaviorMetadata>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching behaviors from {Url}: {Message}",
                $"{_httpClient.BaseAddress}api/behaviors", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching behaviors: {Message}", ex.Message);
            throw;
        }
    }

    // Message Capture endpoints

    public async Task<MessagePagedResult?> QueryMessagesAsync(
        Guid? integrationId = null,
        string? direction = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? correlationId = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        try
        {
            var queryParams = new List<string>();
            if (integrationId.HasValue)
                queryParams.Add($"integrationId={integrationId.Value}");
            if (!string.IsNullOrEmpty(direction))
                queryParams.Add($"direction={direction}");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={status}");
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:O}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:O}");
            if (!string.IsNullOrEmpty(correlationId))
                queryParams.Add($"correlationId={correlationId}");
            queryParams.Add($"pageNumber={pageNumber}");
            queryParams.Add($"pageSize={pageSize}");

            var query = string.Join("&", queryParams);
            var url = $"api/messages?{query}";

            _logger.LogInformation("Querying messages from {Url}", url);
            return await _httpClient.GetFromJsonAsync<MessagePagedResult>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying messages: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CapturedMessageDto?> GetMessageByIdAsync(string messageId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CapturedMessageDto>($"api/messages/{messageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching message {MessageId}: {Message}", messageId, ex.Message);
            throw;
        }
    }

    public async Task<MessageStatisticsDto?> GetMessageStatisticsAsync(Guid integrationId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:O}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:O}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"api/messages/statistics/{integrationId}{query}";

            return await _httpClient.GetFromJsonAsync<MessageStatisticsDto>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching message statistics for integration {IntegrationId}: {Message}",
                integrationId, ex.Message);
            throw;
        }
    }
}
