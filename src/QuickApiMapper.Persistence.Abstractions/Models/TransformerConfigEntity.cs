namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing a transformer configuration applied to a field mapping.
/// </summary>
public class TransformerConfigEntity
{
    /// <summary>
    /// Unique identifier for the transformer configuration.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent field mapping.
    /// </summary>
    public Guid FieldMappingId { get; set; }

    /// <summary>
    /// Name of the transformer to apply (e.g., "formatPhone", "booleanToYN").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Order in which this transformer should be applied in the pipeline.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional JSON-serialized arguments for the transformer.
    /// </summary>
    public string? Arguments { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent field mapping.
    /// </summary>
    public FieldMappingEntity? FieldMapping { get; set; }
}
