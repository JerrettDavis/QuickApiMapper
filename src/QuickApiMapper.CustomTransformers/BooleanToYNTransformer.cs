using JetBrains.Annotations;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.CustomTransformers;

/// <summary>
/// Transforms boolean values to Y/N string representation.
/// Commonly used for systems that expect Y/N instead of true/false.
/// </summary>
[UsedImplicitly]
public sealed class BooleanToYNTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "booleanToYN";

    /// <summary>
    /// Transforms a boolean input value to Y/N string representation.
    /// </summary>
    /// <param name="input">The input value to transform. Should be a boolean string ("true"/"false").</param>
    /// <param name="args">Additional arguments (not used by this transformer).</param>
    /// <returns>"Y" for true values, "N" for false values, or empty string for invalid input.</returns>
    /// <remarks>
    /// This transformer is case-insensitive and handles various boolean representations.
    /// Invalid or null inputs return an empty string.
    /// </remarks>
    public string Transform(
        string? input,
        IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrWhiteSpace(input) ||
            !bool.TryParse(input, out var boolValue))
            return string.Empty;

        return boolValue ? "Y" : "N";
    }
}