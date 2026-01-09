namespace QuickApiMapper.Designer.Web.Utilities;

/// <summary>
/// Generates realistic sample values for field names in schema examples.
/// </summary>
public static class SampleValueGenerator
{
    /// <summary>
    /// Gets a sample value based on the field name.
    /// </summary>
    /// <param name="fieldName">The name of the field to generate a sample value for.</param>
    /// <returns>A realistic sample value.</returns>
    public static string GetSampleValue(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return "sample_value";

        var lowerName = fieldName.ToLowerInvariant();

        // Email patterns
        if (lowerName.Contains("email"))
            return "user@example.com";

        // Name patterns
        if (lowerName.Contains("name") || lowerName.Contains("customer") || lowerName.Contains("user"))
            return lowerName.Contains("company") || lowerName.Contains("business") ? "Acme Corporation" : "John Doe";

        // ID patterns
        if (lowerName.Contains("id") || lowerName.EndsWith("number"))
            return "12345";

        // Date/Time patterns
        if (lowerName.Contains("date") || lowerName.Contains("time") || lowerName.Contains("timestamp"))
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Address patterns
        if (lowerName.Contains("street") || lowerName.Contains("address"))
            return "123 Main Street";

        if (lowerName.Contains("city"))
            return "Springfield";

        if (lowerName.Contains("state") || lowerName.Contains("province"))
            return "IL";

        if (lowerName.Contains("zip") || lowerName.Contains("postal"))
            return "62701";

        if (lowerName.Contains("country"))
            return "USA";

        // Phone patterns
        if (lowerName.Contains("phone") || lowerName.Contains("tel"))
            return "+1-555-123-4567";

        // Price/Amount patterns
        if (lowerName.Contains("price") || lowerName.Contains("amount") || lowerName.Contains("total"))
            return "99.99";

        // Quantity patterns
        if (lowerName.Contains("quantity") || lowerName.Contains("qty") || lowerName.Contains("count"))
            return "1";

        // Status patterns
        if (lowerName.Contains("status"))
            return "ACTIVE";

        // Priority patterns
        if (lowerName.Contains("priority"))
            return "NORMAL";

        // Boolean patterns
        if (lowerName.Contains("is") || lowerName.Contains("has") || lowerName.Contains("enabled"))
            return "true";

        // URL patterns
        if (lowerName.Contains("url") || lowerName.Contains("link"))
            return "https://example.com";

        // Code/SKU patterns
        if (lowerName.Contains("code") || lowerName.Contains("sku"))
            return "PROD-001";

        // Description patterns
        if (lowerName.Contains("description") || lowerName.Contains("notes") || lowerName.Contains("message"))
            return "Sample description text";

        // Default fallback
        return "sample_value";
    }
}
