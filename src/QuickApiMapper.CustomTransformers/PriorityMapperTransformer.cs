using JetBrains.Annotations;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.CustomTransformers;

/// <summary>
/// Transforms e-commerce priority values to warehouse codes.
/// Maps standard priority levels to internal warehouse system codes.
/// </summary>
[UsedImplicitly]
public sealed class PriorityMapperTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "priorityMapper";

    /// <summary>
    /// Transforms e-commerce priority values to warehouse codes.
    /// </summary>
    /// <param name="input">The input priority value (e.g., "STANDARD", "EXPRESS", "OVERNIGHT").</param>
    /// <param name="args">Additional arguments (not used by this transformer).</param>
    /// <returns>The corresponding warehouse code, or "STD" as the default for unknown values.</returns>
    /// <remarks>
    /// This transformer maps priority values as follows:
    /// - "STANDARD" → "STD"
    /// - "EXPRESS" → "EXP"
    /// - "OVERNIGHT" → "OVN"
    /// - Any other value → "STD" (default)
    ///
    /// The comparison is case-insensitive.
    /// Null or empty inputs return "STD" as the default.
    ///
    /// Examples:
    /// - "STANDARD" → "STD"
    /// - "express" → "EXP"
    /// - "OVERNIGHT" → "OVN"
    /// - "UNKNOWN" → "STD"
    /// - null → "STD"
    /// </remarks>
    public string Transform(
        string? input,
        IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "STD";

        return input.ToUpperInvariant() switch
        {
            "STANDARD" => "STD",
            "EXPRESS" => "EXP",
            "OVERNIGHT" => "OVN",
            _ => "STD"
        };
    }
}
