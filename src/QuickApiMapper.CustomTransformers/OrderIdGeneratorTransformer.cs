using JetBrains.Annotations;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.CustomTransformers;

/// <summary>
/// Transforms order IDs by adding configurable prefix and/or suffix.
/// Commonly used to standardize order IDs across different systems.
/// </summary>
[UsedImplicitly]
public sealed class OrderIdGeneratorTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "orderIdGenerator";

    /// <summary>
    /// Transforms an order ID by adding prefix and/or suffix based on arguments.
    /// </summary>
    /// <param name="input">The base order ID to transform (e.g., "12345").</param>
    /// <param name="args">
    /// Optional arguments dictionary. Supported keys:
    /// - "prefix": Text to add before the order ID
    /// - "suffix": Text to add after the order ID
    /// If no arguments are provided, the input is returned unchanged.
    /// </param>
    /// <returns>
    /// The transformed order ID with prefix and/or suffix applied, or empty string if input is null/empty.
    /// </returns>
    /// <remarks>
    /// This transformer allows flexible order ID formatting by combining:
    /// - An optional prefix
    /// - The original order ID
    /// - An optional suffix
    ///
    /// The transformer handles null/empty inputs gracefully and returns an empty string.
    ///
    /// Examples:
    /// - Input: "12345", prefix: "ORD-2026-" → "ORD-2026-12345"
    /// - Input: "12345", suffix: "-WEB" → "12345-WEB"
    /// - Input: "12345", prefix: "ORD-", suffix: "-2026" → "ORD-12345-2026"
    /// - Input: "12345", no args → "12345"
    /// - Input: null → ""
    /// - Input: "" → ""
    ///
    /// Common use cases:
    /// - Adding year/system prefixes: "ORD-2026-12345"
    /// - Adding channel suffixes: "12345-WEB", "12345-MOBILE"
    /// - Full formatting: "ORD-2026-12345-PROCESSED"
    /// </remarks>
    public string Transform(
        string? input,
        IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var prefix = string.Empty;
        var suffix = string.Empty;

        // Extract prefix if provided
        if (args?.TryGetValue("prefix", out var prefixValue) == true &&
            !string.IsNullOrWhiteSpace(prefixValue))
        {
            prefix = prefixValue;
        }

        // Extract suffix if provided
        if (args?.TryGetValue("suffix", out var suffixValue) == true &&
            !string.IsNullOrWhiteSpace(suffixValue))
        {
            suffix = suffixValue;
        }

        // Combine prefix + input + suffix
        return $"{prefix}{input}{suffix}";
    }
}
