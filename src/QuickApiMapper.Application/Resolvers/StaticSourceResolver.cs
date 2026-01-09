using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Resolvers;

/// <summary>
/// Static values source resolver that implements the generic ISourceResolver interface.
/// This replaces the hardcoded static resolver with a generic approach.
/// </summary>
public sealed class StaticSourceResolver :
    ISourceResolver<IReadOnlyDictionary<string, string>>
{
    public IReadOnlyList<string> SupportedTokens => ["$$."];

    public bool CanResolve(string sourcePath) =>
        SupportedTokens.Any(sourcePath.StartsWith);

    public string? Resolve(
        string sourcePath,
        IReadOnlyDictionary<string, string> source,
        IReadOnlyDictionary<string, string>? staticValues = null)
    {
        try
        {
            // Remove the "$$." prefix to get the key
            var key = sourcePath[3..];

            // Look up the value in the static dictionary
            return source.GetValueOrDefault(key);
        }
        catch (Exception)
        {
            return null;
        }
    }
}