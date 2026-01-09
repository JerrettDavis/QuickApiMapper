using System.Xml.Linq;
using System.Xml.XPath;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Resolvers;

/// <summary>
/// Resolves XML XPath expressions (/path) against XDocument sources.
/// Supports standard XPath syntax for element and attribute selection.
/// </summary>
public sealed class XmlSourceResolver : ISourceResolver<XDocument>
{
    /// <summary>
    /// Gets the supported path tokens that this resolver can handle.
    /// </summary>
    public IReadOnlyList<string> SupportedTokens => ["/", "xml:/"];

    /// <summary>
    /// Determines if this resolver can handle the specified source path.
    /// </summary>
    /// <param name="sourcePath">The source path to check.</param>
    /// <returns>True if the path starts with "/", false otherwise.</returns>
    public bool CanResolve(string sourcePath) => SupportedTokens.Any(sourcePath.StartsWith);

    /// <summary>
    /// Resolves an XPath expression against the provided XDocument source.
    /// </summary>
    /// <param name="sourcePath">The XPath expression (e.g., "/root/user/@name", "/items/item[1]/value").</param>
    /// <param name="xml">The XDocument to query against.</param>
    /// <param name="statics">Static values (not used by this resolver).</param>
    /// <returns>The resolved value as a string, or null if not found.</returns>
    public string? Resolve(
        string sourcePath,
        XDocument? xml,
        IReadOnlyDictionary<string, string>? statics)
    {
        if (xml?.Root == null)
            return null;
        
        sourcePath = CleanXPath(sourcePath);

        try
        {
            // Handle attribute selection (ends with @attribute)
            if (sourcePath.Contains("/@"))
            {
                var attrIndex = sourcePath.LastIndexOf("/@", StringComparison.Ordinal);
                var elementPath = sourcePath[..attrIndex];
                var attributeName = sourcePath[(attrIndex + 2)..];

                var element = xml.XPathSelectElement(elementPath);
                return element?.Attribute(attributeName)?.Value;
            }

            // Handle element selection
            var selectedElement = xml.XPathSelectElement(sourcePath);
            return selectedElement?.Value;
        }
        catch (XPathException)
        {
            // Return null for invalid XPath expressions
            return null;
        }
        catch (Exception)
        {
            // Return null for any other XML processing errors
            return null;
        }
    }

    
    private static string CleanXPath(string xpath)
        // Remove leading slashes and ensure valid XPath format
        => xpath.TrimStart('/').Replace("xml:/", "/").Replace("//", "/");
}