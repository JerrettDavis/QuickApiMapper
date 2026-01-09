namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing SOAP-specific configuration for an integration.
/// </summary>
public class SoapConfigEntity
{
    /// <summary>
    /// Unique identifier for the SOAP configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping.
    /// </summary>
    public Guid IntegrationMappingId { get; set; }

    /// <summary>
    /// XPath for the body wrapper field (e.g., "SendSynchronic2").
    /// </summary>
    public string? BodyWrapperFieldXPath { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent integration mapping.
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }

    /// <summary>
    /// Collection of SOAP header and body fields.
    /// </summary>
    public List<SoapFieldEntity> Fields { get; set; } = [];
}
