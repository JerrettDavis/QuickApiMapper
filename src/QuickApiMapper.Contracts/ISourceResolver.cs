namespace QuickApiMapper.Contracts;

/// <summary>
/// Generic source resolver that can work with any source type.
/// </summary>
/// <typeparam name="TSource">The source type to resolve values from.</typeparam>
public interface ISourceResolver<TSource> where TSource : class
{
    /// <summary>
    /// The supported token prefixes that this resolver can handle.
    /// </summary>
    IReadOnlyList<string> SupportedTokens { get; }

    /// <summary>
    /// Determines if this resolver can handle the given source path.
    /// </summary>
    /// <param name="sourcePath">The source path to check.</param>
    /// <returns>True if this resolver can handle the path.</returns>
    bool CanResolve(string sourcePath);

    /// <summary>
    /// Resolves a value from the source using the specified path.
    /// </summary>
    /// <param name="sourcePath">The path to resolve.</param>
    /// <param name="source">The source object to resolve from.</param>
    /// <param name="staticValues">Static values for fallback resolution.</param>
    /// <returns>The resolved value or null if not found.</returns>
    string? Resolve(string sourcePath, TSource source, IReadOnlyDictionary<string, string>? staticValues = null);
}