namespace QuickApiMapper.Contracts;

/// <summary>
/// Represents a transformer configuration.
/// </summary>
public record Transformer(
    string Name,
    IReadOnlyDictionary<string, string?>? Args = null
);