namespace QuickApiMapper.Management.Api.Models;

/// <summary>
/// Request model for creating a new integration.
/// </summary>
public class CreateIntegrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EnableInput { get; set; } = true;
    public bool EnableOutput { get; set; } = true;
    public bool EnableMessageCapture { get; set; } = true;

    public List<FieldMappingDto>? FieldMappings { get; set; }
    public Dictionary<string, string>? StaticValues { get; set; }
    public SoapConfigDto? SoapConfig { get; set; }
}

/// <summary>
/// Request model for updating an existing integration.
/// </summary>
public class UpdateIntegrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EnableInput { get; set; } = true;
    public bool EnableOutput { get; set; } = true;
    public bool EnableMessageCapture { get; set; } = true;

    public List<FieldMappingDto>? FieldMappings { get; set; }
    public Dictionary<string, string>? StaticValues { get; set; }
    public SoapConfigDto? SoapConfig { get; set; }
}

/// <summary>
/// Request model for testing an integration with sample data.
/// </summary>
public class TestMappingRequest
{
    public string SamplePayload { get; set; } = string.Empty;
    public Dictionary<string, string>? OverrideStaticValues { get; set; }
}

/// <summary>
/// Response model for testing an integration.
/// </summary>
public class TestMappingResponse
{
    public bool Success { get; set; }
    public string? TransformedPayload { get; set; }
    public string? Errors { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public List<TransformationStep>? Steps { get; set; }
}
