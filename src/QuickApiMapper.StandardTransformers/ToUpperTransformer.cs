using QuickApiMapper.Contracts;

namespace QuickApiMapper.StandardTransformers;

/// <summary>
/// Transforms string values to uppercase.
/// Commonly used for standardizing text data for storage or comparison.
/// </summary>
public sealed class ToUpperTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "toUpper";

    /// <summary>
    /// Transforms a string to uppercase using invariant culture.
    /// </summary>
    /// <param name="input">The input string to transform.</param>
    /// <param name="args">Additional arguments (not used by this transformer).</param>
    /// <returns>The uppercase version of the input string, or empty string if input is null.</returns>
    /// <remarks>
    /// This transformer uses invariant culture for consistent results across different locales.
    /// Examples:
    /// - "hello world" → "HELLO WORLD"
    /// - "Mixed Case" → "MIXED CASE"
    /// - null → ""
    /// </remarks>
    public string Transform(string? input, IReadOnlyDictionary<string, string?>? args)
    {
        return input?.ToUpperInvariant() ?? string.Empty;
    }
}