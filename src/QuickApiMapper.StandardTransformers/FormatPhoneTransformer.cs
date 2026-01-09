using QuickApiMapper.Contracts;

namespace QuickApiMapper.StandardTransformers;

/// <summary>
/// Transforms phone numbers by removing non-digit characters.
/// Commonly used to normalize phone numbers for storage or API calls.
/// </summary>
public sealed class FormatPhoneTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "formatPhone";

    /// <summary>
    /// Transforms a phone number by extracting only the digit characters.
    /// </summary>
    /// <param name="input">The input phone number to transform (e.g., "(555) 123-4567").</param>
    /// <param name="args">Additional arguments (not used by this transformer).</param>
    /// <returns>A string containing only the digit characters from the input, or empty string if input is null.</returns>
    /// <remarks>
    /// This transformer removes all non-digit characters including spaces, parentheses, hyphens, and plus signs.
    /// Examples:
    /// - "(555) 123-4567" → "5551234567"
    /// - "+1-555-123-4567" → "15551234567"
    /// - "555.123.4567" → "5551234567"
    /// </remarks>
    public string Transform(
        string? input,
        IReadOnlyDictionary<string, string?>? args)
    {
        if (input is null)
            return string.Empty;
        
        if (!input.Any(char.IsDigit))
            return input;

        // Extract only digit characters
        var digits = new string([.. input.Where(char.IsDigit)]);
        return digits;
    }
}