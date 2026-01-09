namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing a SOAP header or body field configuration.
/// </summary>
public class SoapFieldEntity
{
    /// <summary>
    /// Unique identifier for the SOAP field.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent SOAP configuration.
    /// </summary>
    public Guid SoapConfigId { get; set; }

    /// <summary>
    /// Type of field: "Header" or "Body".
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// XPath for this field (e.g., "WrapperHeader/User").
    /// </summary>
    public string XPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional source expression to populate this field's value.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Optional XML namespace for this field.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Optional namespace prefix.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// JSON-serialized attributes for this field.
    /// </summary>
    public string? Attributes { get; set; }

    /// <summary>
    /// Order in which this field should be processed.
    /// </summary>
    public int Order { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent SOAP configuration.
    /// </summary>
    public SoapConfigEntity? SoapConfig { get; set; }
}
