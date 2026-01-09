using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Resolvers;

/// <summary>
/// Resolves JSON path expressions ($.path) against JObject sources.
/// Supports JSONPath syntax including array indexing and filter expressions.
/// </summary>
public sealed class JsonSourceResolver : ISourceResolver<JObject>
{
    /// <summary>
    /// Gets the supported path tokens that this resolver can handle.
    /// </summary>
    public IReadOnlyList<string> SupportedTokens => ["$."];

    /// <summary>
    /// Determines if this resolver can handle the specified source path.
    /// </summary>
    /// <param name="sourcePath">The source path to check.</param>
    /// <returns>True if the path starts with "$.", false otherwise.</returns>
    public bool CanResolve(string sourcePath) => SupportedTokens.Any(sourcePath.StartsWith);

    /// <summary>
    /// Resolves a JSON path expression against the provided JObject source.
    /// </summary>
    /// <param name="sourcePath">The JSON path expression (e.g., "$.user.name", "$.items[0].id").</param>
    /// <param name="json">The JObject to query against.</param>
    /// <param name="statics">Static values (not used by this resolver).</param>
    /// <returns>The resolved value as a string, or null if not found.</returns>
    public string? Resolve(
        string sourcePath,
        JObject? json,
        IReadOnlyDictionary<string, string>? statics)
    {
        if (json == null)
            return null;

        try
        {
            // Use Newtonsoft.Json's JSONPath implementation
            var token = json.SelectToken(sourcePath);
            return token?.ToString();
        }
        catch (Exception)
        {
            // Return null for invalid paths or query errors
            return null;
        }
    }
}