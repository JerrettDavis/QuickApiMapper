namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing a single field mapping within an integration.
/// </summary>
public class FieldMappingEntity
{
    /// <summary>
    /// Unique identifier for the field mapping.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping.
    /// </summary>
    public Guid IntegrationMappingId { get; set; }

    /// <summary>
    /// Source path expression (JSONPath, XPath, or static reference).
    /// Examples: "$.customerinfo[0].customer_id", "/root/session", "$$.Username"
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Destination path expression where the value should be written.
    /// Examples: "$.customerId", "/root/session/user"
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Order in which this mapping should be applied (for deterministic execution).
    /// </summary>
    public int Order { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent integration mapping.
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }

    /// <summary>
    /// Collection of transformers to apply to the mapped value.
    /// </summary>
    public List<TransformerConfigEntity> Transformers { get; set; } = [];
}
