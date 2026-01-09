namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing a static value that can be used in mappings.
/// </summary>
public class StaticValueEntity
{
    /// <summary>
    /// Unique identifier for the static value.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the parent integration mapping (null for global static values).
    /// </summary>
    public Guid? IntegrationMappingId { get; set; }

    /// <summary>
    /// Key name for the static value (e.g., "Username", "Password").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The static value itself.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is a global static value (available to all integrations).
    /// </summary>
    public bool IsGlobal { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent integration mapping (null for global values).
    /// </summary>
    public IntegrationMappingEntity? IntegrationMapping { get; set; }
}
