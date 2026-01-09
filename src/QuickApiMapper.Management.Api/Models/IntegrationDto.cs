namespace QuickApiMapper.Management.Api.Models;

/// <summary>
/// Data transfer object for integration mapping.
/// </summary>
public class IntegrationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EnableInput { get; set; } = true;
    public bool EnableOutput { get; set; } = true;
    public bool EnableMessageCapture { get; set; } = true;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<FieldMappingDto>? FieldMappings { get; set; }
    public Dictionary<string, string>? StaticValues { get; set; }
    public SoapConfigDto? SoapConfig { get; set; }
}

/// <summary>
/// Field mapping DTO.
/// </summary>
public class FieldMappingDto
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public int Order { get; set; }
    public List<TransformerDto>? Transformers { get; set; }
}

/// <summary>
/// Transformer configuration DTO.
/// </summary>
public class TransformerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// SOAP configuration DTO.
/// </summary>
public class SoapConfigDto
{
    public string? BodyWrapperFieldXpath { get; set; }
    public List<SoapFieldDto>? Fields { get; set; }
}

/// <summary>
/// SOAP field DTO.
/// </summary>
public class SoapFieldDto
{
    public string FieldType { get; set; } = string.Empty;
    public string Xpath { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Namespace { get; set; }
    public string? Prefix { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
    public int Order { get; set; }
}
