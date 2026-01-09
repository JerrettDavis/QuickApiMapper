namespace QuickApiMapper.Contracts;

/// <summary>
/// Represents a field mapping configuration.
/// </summary>
public record FieldMapping(
    string Source,
    string? Destination = null,
    IReadOnlyList<Transformer>? Transformers = null
);