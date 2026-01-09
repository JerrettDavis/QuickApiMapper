using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Management.Contracts.Models;

namespace QuickApiMapper.Designer.Web.Utilities;

/// <summary>
/// Generates example JSON and XML documents from field mappings for schema preview.
/// </summary>
public static class SchemaExampleGenerator
{
    /// <summary>
    /// Generates an example JSON document from field mappings.
    /// </summary>
    /// <param name="mappings">The field mappings to generate from.</param>
    /// <param name="staticValues">Static values to substitute for $$ variables.</param>
    /// <param name="isSource">True for source mappings, false for destination mappings.</param>
    /// <returns>Formatted JSON string representing the example document.</returns>
    public static string GenerateJsonExample(List<FieldMappingDto>? mappings, Dictionary<string, string>? staticValues, bool isSource)
    {
        if (mappings == null || !mappings.Any())
            return "{}";

        var root = new JObject();

        // Get relevant JSON paths based on whether we're generating source or destination
        var jsonPaths = mappings
            .Select(m => isSource ? m.Source : m.Destination)
            .Where(p => p != null && p.StartsWith("$.") && !p.StartsWith("$$."))
            .Distinct()
            .ToList();

        // Process each JSON path
        foreach (var jsonPath in jsonPaths)
        {
            ProcessJsonPathForExample(jsonPath!, root, staticValues);
        }

        return root.ToString(Formatting.Indented);
    }

    /// <summary>
    /// Generates an example XML document from field mappings.
    /// </summary>
    /// <param name="mappings">The field mappings to generate from.</param>
    /// <param name="staticValues">Static values to substitute for $$ variables.</param>
    /// <param name="isSource">True for source mappings, false for destination mappings.</param>
    /// <returns>Formatted XML string representing the example document.</returns>
    public static string GenerateXmlExample(List<FieldMappingDto>? mappings, Dictionary<string, string>? staticValues, bool isSource)
    {
        if (mappings == null || !mappings.Any())
            return "<root />";

        // Get relevant XPath expressions based on whether we're generating source or destination
        var xpaths = mappings
            .Select(m => isSource ? m.Source : m.Destination)
            .Where(p => p != null && p.StartsWith("/") && !p.StartsWith("$"))
            .Distinct()
            .OrderBy(p => p)  // Order to build structure logically
            .ToList();

        if (!xpaths.Any())
            return "<root />";

        var doc = new XDocument();

        // Process each XPath
        foreach (var xpath in xpaths)
        {
            ProcessXPathForExample(xpath!, doc, staticValues);
        }

        return doc.ToString();
    }

    private static void ProcessJsonPathForExample(string jsonPath, JObject root, Dictionary<string, string>? staticValues)
    {
        // Remove the initial $. prefix
        if (!jsonPath.StartsWith("$."))
            return;

        var path = jsonPath[2..];

        // Parse the path segments
        var segments = ParseJsonPathSegments(path);

        // Build the example structure
        JToken current = root;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var isLast = i == segments.Count - 1;

            if (segment.IsArray)
            {
                // Handle array property
                if (current is JObject obj)
                {
                    if (!obj.ContainsKey(segment.Name))
                    {
                        // Create array with one example item
                        var arrayItem = new JObject();
                        obj[segment.Name] = new JArray(arrayItem);
                    }

                    // Move to the first item in the array for next segment
                    if (!isLast && obj[segment.Name] is JArray arr && arr.Count > 0)
                    {
                        current = arr[0];
                    }
                }
            }
            else
            {
                // Handle regular object property
                if (current is JObject currentObj)
                {
                    if (isLast)
                    {
                        // Leaf property - set sample value
                        var sampleValue = SampleValueGenerator.GetSampleValue(segment.Name);
                        currentObj[segment.Name] = sampleValue;
                    }
                    else
                    {
                        // Intermediate object
                        if (!currentObj.ContainsKey(segment.Name))
                        {
                            currentObj[segment.Name] = new JObject();
                        }

                        current = currentObj[segment.Name]!;
                    }
                }
            }
        }
    }

    private static void ProcessXPathForExample(string xpath, XDocument doc, Dictionary<string, string>? staticValues)
    {
        if (string.IsNullOrEmpty(xpath) || !xpath.StartsWith("/"))
            return;

        var isAttribute = xpath.Contains("/@");

        if (isAttribute)
        {
            // Handle attribute path (e.g., /root/user/@id)
            var parts = xpath.Split("/@");
            if (parts.Length != 2)
                return;

            var elementPath = parts[0];
            var attributeName = parts[1];

            var element = FindOrCreateElement(elementPath, doc);
            if (element != null)
            {
                var sampleValue = SampleValueGenerator.GetSampleValue(attributeName);
                element.SetAttributeValue(attributeName, sampleValue);
            }
        }
        else
        {
            // Handle element path
            var element = FindOrCreateElement(xpath, doc);
            if (element != null)
            {
                var elementName = element.Name.LocalName;
                var sampleValue = SampleValueGenerator.GetSampleValue(elementName);
                element.Value = sampleValue;
            }
        }
    }

    private static XElement? FindOrCreateElement(string path, XDocument xml)
    {
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
            return null;

        // Ensure root element exists
        if (xml.Root == null)
        {
            xml.Add(new XElement(GetElementName(pathParts[0])));
        }
        else if (xml.Root.Name.LocalName != GetElementName(pathParts[0]))
        {
            // Root doesn't match - for example generation, we'll use the first root we encounter
            // This handles cases where multiple roots are implied (which wouldn't be valid XML anyway)
            if (xml.Root.Elements().Any())
            {
                // Already has content, don't replace
            }
            else
            {
                var newRoot = new XElement(GetElementName(pathParts[0]));
                xml.Root.ReplaceWith(newRoot);
            }
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

    private static XElement FindOrCreateElementWithIndex(XElement parent, string elementSpec)
    {
        var (elementName, index) = ParseElementWithIndex(elementSpec);

        // Find existing elements with this name
        var existingElements = parent.Elements(elementName).ToList();

        if (index.HasValue)
        {
            // Handle indexed access - for examples, we typically just need one item
            var targetIndex = Math.Min(index.Value, 0); // Use first item for examples

            if (existingElements.Count > targetIndex)
            {
                return existingElements[targetIndex];
            }

            // Create the element if it doesn't exist
            var newElement = new XElement(elementName);
            parent.Add(newElement);
            return newElement;
        }

        // Handle non-indexed access - find existing or create new
        var existingElement = existingElements.FirstOrDefault();
        if (existingElement != null)
            return existingElement;

        existingElement = new XElement(elementName);
        parent.Add(existingElement);

        return existingElement;
    }

    private static (string elementName, int? index) ParseElementWithIndex(string elementSpec)
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

    private static string GetElementName(string elementSpec)
    {
        var (elementName, _) = ParseElementWithIndex(elementSpec);
        return elementName;
    }

    private static List<PathSegment> ParseJsonPathSegments(string path)
    {
        var segments = new List<PathSegment>();

        // Split by dots, but we need to handle array brackets more carefully
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        var bracketDepth = 0;

        for (var i = 0; i < path.Length; i++)
        {
            var ch = path[i];

            if (ch == '[')
            {
                bracketDepth++;
                currentPart.Append(ch);
            }
            else if (ch == ']')
            {
                bracketDepth--;
                currentPart.Append(ch);
            }
            else if (ch == '.' && bracketDepth == 0)
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
            }
            else
            {
                currentPart.Append(ch);
            }
        }

        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            // Check if this segment has array notation
            if (part.Contains('['))
            {
                var arrayMatch = Regex.Match(part, @"^([^[]+)\[");
                if (arrayMatch.Success)
                {
                    var arrayName = arrayMatch.Groups[1].Value;
                    segments.Add(new PathSegment(arrayName, true));
                }
            }
            else
            {
                segments.Add(new PathSegment(part, false));
            }
        }

        return segments;
    }

    private record PathSegment(string Name, bool IsArray);
}
