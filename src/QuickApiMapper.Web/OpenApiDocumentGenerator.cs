using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper;

public class OpenApiDocumentGenerator(ApiMappingConfig config)
{
    public JObject GenerateOpenApiDocument()
    {
        var paths = new JObject();
        foreach (var integration in config.Mappings ?? [])
        {
            var schema = SynthesizeJsonSchema(integration.Mapping);
            var pathItem = new JObject
            {
                ["post"] = new JObject
                {
                    ["tags"] = new JArray("QuickApiMapper"),
                    ["operationId"] = integration.Name,
                    ["summary"] = $"Process {integration.Name} integration",
                    ["requestBody"] = new JObject
                    {
                        ["content"] = new JObject
                        {
                            ["application/json"] = new JObject
                            {
                                ["schema"] = schema
                            }
                        },
                        ["required"] = true
                    },
                    ["responses"] = new JObject
                    {
                        ["200"] = new JObject
                        {
                            ["description"] = "Success",
                            ["content"] = new JObject
                            {
                                ["application/xml"] = new JObject
                                {
                                    ["schema"] = new JObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "XML response from destination system"
                                    }
                                }
                            }
                        },
                        ["400"] = new JObject
                        {
                            ["description"] = "Bad Request"
                        },
                        ["500"] = new JObject
                        {
                            ["description"] = "Internal Server Error"
                        }
                    }
                }
            };
            paths[integration.Endpoint] = pathItem;
        }

        return new JObject
        {
            ["openapi"] = "3.0.1",
            ["info"] = new JObject
            {
                ["title"] = "QuickApiMapper API",
                ["version"] = "1.0.0",
                ["description"] = "API for mapping and transforming data between different systems"
            },
            ["servers"] = GetServersFromUrls(),
            ["paths"] = paths,
            ["components"] = new JObject
            {
                ["schemas"] = new JObject()
            },
            ["tags"] = new JArray(new JObject { ["name"] = "QuickApiMapper" })
        };
    }

    private static JArray GetServersFromUrls()
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?
            .Split(';', StringSplitOptions.RemoveEmptyEntries) ?? ["http://localhost:5072"];
        var servers = new JArray();
        foreach (var url in urls)
            servers.Add(new JObject { ["url"] = url });
        return servers;
    }


    public static JObject SynthesizeJsonSchema(IEnumerable<FieldMapping>? mappings)
    {
        var root = new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject()
        };

        var rootProps = (JObject)root["properties"]!;

        // Get all JSON paths from mappings (excluding static values that start with $$)
        // Include both mappings with and without destinations since they're all part of the input
        var jsonPaths = mappings?
            .Select(m => m.Source)
            .Where(s => s.StartsWith("$.") && !s.StartsWith("$$."))
            .Distinct()
            .ToList() ?? [];

        // Parse each JSON path and build the schema structure
        foreach (var jsonPath in jsonPaths)
            ProcessJsonPath(jsonPath, rootProps);

        return root;
    }

    private static void ProcessJsonPath(string jsonPath, JObject rootProps)
    {
        // Remove the initial $. prefix
        var path = jsonPath[2..];

        // Parse the path segments
        var segments = ParsePathSegments(path);

        // Build the schema structure
        var current = rootProps;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var isLast = i == segments.Count - 1;

            if (segment.IsArray)
            {
                // Handle array property
                if (!current.ContainsKey(segment.Name))
                {
                    current[segment.Name] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = new JObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JObject()
                        }
                    };
                }

                // Add any filter fields to the array item properties
                if (segment.FilterFields != null)
                {
                    var itemProps = (JObject)current[segment.Name]!["items"]!["properties"]!;
                    foreach (var filterField in segment.FilterFields)
                    {
                        if (!itemProps.ContainsKey(filterField))
                        {
                            itemProps[filterField] = new JObject { ["type"] = "string" };
                        }
                    }
                }

                // Move to the items.properties for the next segment
                if (!isLast)
                {
                    current = (JObject)current[segment.Name]!["items"]!["properties"]!;
                }
            }
            else
            {
                // Handle regular object property
                if (!current.ContainsKey(segment.Name))
                {
                    if (isLast)
                    {
                        // Leaf property - determine type based on common field names
                        var propertyType = InferPropertyType(segment.Name);
                        current[segment.Name] = new JObject { ["type"] = propertyType };

                        // Add nullable for fields that might be null
                        if (ShouldBeNullable(segment.Name))
                        {
                            current[segment.Name]!["nullable"] = true;
                        }
                    }
                    else
                    {
                        // Intermediate object
                        current[segment.Name] = new JObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JObject()
                        };
                    }
                }

                // Move to the properties for the next segment
                if (!isLast)
                {
                    current = (JObject)current[segment.Name]!["properties"]!;
                }
            }
        }
    }

    private static List<PathSegment> ParsePathSegments(string path)
    {
        var segments = new List<PathSegment>();

        // Split by dots, but we need to handle the filter expressions more carefully
        var parts = new List<string>();
        var currentPart = "";
        var bracketDepth = 0;

        for (var i = 0; i < path.Length; i++)
        {
            var ch = path[i];

            if (ch == '[')
            {
                bracketDepth++;
                currentPart += ch;
            }
            else if (ch == ']')
            {
                bracketDepth--;
                currentPart += ch;
            }
            else if (ch == '.' && bracketDepth == 0)
            {
                if (!string.IsNullOrEmpty(currentPart))
                {
                    parts.Add(currentPart);
                    currentPart = "";
                }
            }
            else
            {
                currentPart += ch;
            }
        }

        if (!string.IsNullOrEmpty(currentPart))
        {
            parts.Add(currentPart);
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

                    // Extract filter fields from expressions like [?(@.comm_type == 'Email')]
                    var filterFields = new List<string>();

                    // Look for filter expressions in the format [?(@.field_name == 'value')]
                    var filterMatch = Regex.Match(part, @"\?\(@\.([a-zA-Z_][a-zA-Z0-9_]*)\s*==\s*'[^']*'\)");
                    if (filterMatch.Success)
                    {
                        var filterField = filterMatch.Groups[1].Value;
                        filterFields.Add(filterField);
                    }

                    segments.Add(new PathSegment(arrayName, true, filterFields.Count > 0 ? filterFields : null));
                }
            }
            else
            {
                segments.Add(new PathSegment(part, false));
            }
        }

        return segments;
    }

    private static string InferPropertyType(string _)
    {
        // For now, assume all properties are strings as requested.
        // Later we can add SourceType configuration to override this behavior
        return "string";
    }

    private static bool ShouldBeNullable(string fieldName)
    {
        var lowerName = fieldName.ToLowerInvariant();
        return lowerName.Contains("message_id") || lowerName.Contains("county") ||
               lowerName.Contains("address_2") || lowerName.Contains("optional");
    }

    private record PathSegment(string Name, bool IsArray, List<string>? FilterFields = null);
}