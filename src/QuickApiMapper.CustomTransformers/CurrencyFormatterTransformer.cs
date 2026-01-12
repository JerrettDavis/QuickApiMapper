using System.Globalization;
using JetBrains.Annotations;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.CustomTransformers;

/// <summary>
/// Formats currency values with proper decimal places and currency codes.
/// Supports multiple currency types (USD, EUR, GBP) with appropriate formatting.
/// </summary>
[UsedImplicitly]
public sealed class CurrencyFormatterTransformer : ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    public string Name => "currencyFormatter";

    /// <summary>
    /// Formats a numeric value as a currency string with the specified currency code.
    /// </summary>
    /// <param name="input">The numeric value to format (e.g., "599.99", "1234.5").</param>
    /// <param name="args">
    /// Optional arguments dictionary. Supported keys:
    /// - "currency": The currency code (USD, EUR, GBP). Defaults to "USD" if not specified.
    /// </param>
    /// <returns>
    /// A formatted currency string (e.g., "599.99 USD"), or empty string if input is invalid.
    /// </returns>
    /// <remarks>
    /// This transformer formats numeric values with proper decimal precision:
    /// - All currencies are formatted with 2 decimal places
    /// - The currency code is appended to the formatted value
    /// - Uses invariant culture for consistent decimal formatting
    ///
    /// Supported currencies:
    /// - USD (US Dollar)
    /// - EUR (Euro)
    /// - GBP (British Pound)
    /// - Defaults to USD for any other currency code
    ///
    /// Examples:
    /// - Input: "599.99", currency: "USD" → "599.99 USD"
    /// - Input: "1234.5", currency: "EUR" → "1234.50 EUR"
    /// - Input: "42", currency: "GBP" → "42.00 GBP"
    /// - Input: "invalid" → ""
    /// - Input: null → ""
    /// </remarks>
    public string Transform(
        string? input,
        IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return string.Empty;

        // Get currency code from args, default to USD
        var currencyCode = "USD";
        if (args?.TryGetValue("currency", out var currency) == true &&
            !string.IsNullOrWhiteSpace(currency))
        {
            currencyCode = currency.ToUpperInvariant();
        }

        // Validate currency code (support USD, EUR, GBP)
        if (currencyCode is not ("USD" or "EUR" or "GBP"))
        {
            currencyCode = "USD";
        }

        // Format with 2 decimal places using invariant culture
        var formattedValue = value.ToString("0.00", CultureInfo.InvariantCulture);
        return $"{formattedValue} {currencyCode}";
    }
}
