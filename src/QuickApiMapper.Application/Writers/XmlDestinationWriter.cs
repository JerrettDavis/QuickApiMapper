using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Writers;

/// <summary>
/// Writes values to XML document destinations using XPath-style syntax.
/// Supports element and attribute writing with automatic node creation.
/// </summary>
public sealed class XmlDestinationWriter(
    ILogger<XmlDestinationWriter> logger) : 
    IDestinationWriter<XDocument>
{
    private readonly ILogger<XmlDestinationWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IReadOnlyList<string> SupportedTokens => ["/"];

    /// <summary>
    /// Determines if this writer can handle the specified destination path.
    /// </summary>
    /// <param name="destPath">The destination path to check.</param>
    /// <returns>True if the path starts with "/", false otherwise.</returns>
    public bool CanWrite(string destPath) => SupportedTokens.Any(destPath.StartsWith);

    /// <summary>
    /// Writes a value to the specified XPath location in the target XML document.
    /// </summary>
    /// <param name="destPath">The XPath where to write the value (e.g., "/root/user", "/root/user/@name").</param>
    /// <param name="value">The value to write.</param>
    /// <param name="xml">The target XML document.</param>
    /// <returns>True if the write was successful, false otherwise.</returns>
    public bool Write(string destPath, string? value, XDocument xml)
    {
        if (string.IsNullOrEmpty(destPath) || !destPath.StartsWith("/"))
        {
            _logger.LogError("Invalid XPath format. Must start with '/'");
            return false;
        }

        try
        {
            var isAttribute = destPath.Contains("/@");

            return isAttribute 
                ? WriteToAttribute(destPath, value, xml) 
                : WriteToElement(destPath, value, xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to XML destination path: {Path}", destPath);
            return false;
        }
    }

    /// <summary>
    /// Writes a value to an XML element.
    /// </summary>
    private bool WriteToElement(string destPath, string? value, XDocument xml)
    {
        var element = FindOrCreateElement(destPath, xml);
        if (element == null)
        {
            return false;
        }

        element.Value = value ?? string.Empty;
        _logger.LogDebug("Successfully wrote value to XML element: {Path} = {Value}", destPath, value);
        return true;
    }

    /// <summary>
    /// Writes a value to an XML attribute.
    /// </summary>
    private bool WriteToAttribute(string destPath, string? value, XDocument xml)
    {
        var parts = destPath.Split("/@");
        if (parts.Length != 2)
        {
            _logger.LogError("Invalid attribute path format: {Path}", destPath);
            return false;
        }

        var elementPath = parts[0];
        var attributeName = parts[1];

        var element = FindOrCreateElement(elementPath, xml);
        if (element == null)
        {
            return false;
        }

        element.SetAttributeValue(attributeName, value);
        _logger.LogDebug("Successfully wrote value to XML attribute: {Path} = {Value}", destPath, value);
        return true;
    }

    /// <summary>
    /// Finds or creates an XML element at the specified path.
    /// </summary>
    private XElement? FindOrCreateElement(string path, XDocument xml)
    {
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
            return null;

        // Ensure root element exists
        if (xml.Root == null)
            xml.Add(new XElement(pathParts[0]));
        else if (xml.Root.Name.LocalName != GetElementName(pathParts[0]))
        {
            // If root doesn't match, we need to create a new structure
            var newRoot = new XElement(GetElementName(pathParts[0]));
            xml.Root.ReplaceWith(newRoot);
        }

        var current = xml.Root!;

        // Navigate/create the remaining path
        for (var i = 1; i < pathParts.Length; i++)
        {
            var part = pathParts[i];
            current = FindOrCreateElementWithIndex(current, part);
        }

        return current;
    }

    /// <summary>
    /// Finds or creates an element with support for indexed notation (e.g., "item[0]").
    /// </summary>
    private XElement FindOrCreateElementWithIndex(XElement parent, string elementSpec)
    {
        ArgumentNullException.ThrowIfNull(parent);
        
        var (elementName, index) = ParseElementWithIndex(elementSpec);

        // Find existing elements with this name
        var existingElements = parent.Elements(elementName).ToList();

        if (index.HasValue)
        {
            // Handle indexed access
            var targetIndex = index.Value;

            // Create missing elements up to the target index
            while (existingElements.Count <= targetIndex)
            {
                var newElement = new XElement(elementName);
                parent.Add(newElement);
                existingElements.Add(newElement);
            }

            return existingElements[targetIndex];
        }

        // Handle non-indexed access - find existing or create new
        var existingElement = existingElements.FirstOrDefault();
        if (existingElement != null)
            return existingElement;
            
        existingElement = new XElement(elementName);
        parent.Add(existingElement);

        return existingElement;
    }

    /// <summary>
    /// Parses an element specification that may include an index (e.g., "item[0]").
    /// </summary>
    private (string elementName, int? index) ParseElementWithIndex(string elementSpec)
    {
        var indexMatch = Regex.Match(elementSpec, @"^(.+?)\[(\d+)\]$");
        if (indexMatch.Success)
        {
            var elementName = indexMatch.Groups[1].Value;
            var index = int.Parse(indexMatch.Groups[2].Value);
            return (elementName, index);
        }

        return (elementSpec, null);
    }

    /// <summary>
    /// Extracts the element name from an element specification (removing any index).
    /// </summary>
    private string GetElementName(string elementSpec)
    {
        var (elementName, _) = ParseElementWithIndex(elementSpec);
        return elementName;
    }
}