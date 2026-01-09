using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Extensions.gRPC.Resolvers;

/// <summary>
/// Source resolver for extracting data from gRPC (Protobuf) messages.
/// Supports field paths similar to JSONPath but adapted for Protobuf structure.
/// </summary>
/// <remarks>
/// Field path syntax:
/// - "fieldName" - Top-level field
/// - "message.field" - Nested field
/// - "repeated[0]" - First element of repeated field
/// - "map[key]" - Map field access
/// </remarks>
public class GrpcSourceResolver : ISourceResolver<IMessage>
{
    private readonly ILogger<GrpcSourceResolver> _logger;

    public GrpcSourceResolver(ILogger<GrpcSourceResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<string> SupportedTokens => new[] { "$grpc", "$protobuf", "$static" };

    public bool CanResolve(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return false;

        // Can resolve static values or any gRPC field path
        return sourcePath.StartsWith("$static:", StringComparison.OrdinalIgnoreCase) ||
               sourcePath.StartsWith("$grpc:", StringComparison.OrdinalIgnoreCase) ||
               sourcePath.StartsWith("$protobuf:", StringComparison.OrdinalIgnoreCase) ||
               !sourcePath.StartsWith("$"); // Plain field paths without prefix
    }

    public string? Resolve(
        string sourcePath,
        IMessage source,
        IReadOnlyDictionary<string, string>? staticValues = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.LogWarning("Source path is null or empty");
            return null;
        }

        // Remove protocol prefix if present
        if (sourcePath.StartsWith("$grpc:", StringComparison.OrdinalIgnoreCase))
        {
            sourcePath = sourcePath.Substring("$grpc:".Length);
        }
        else if (sourcePath.StartsWith("$protobuf:", StringComparison.OrdinalIgnoreCase))
        {
            sourcePath = sourcePath.Substring("$protobuf:".Length);
        }

        // Handle static value references (e.g., "$static:ApiKey")
        if (sourcePath.StartsWith("$static:", StringComparison.OrdinalIgnoreCase))
        {
            var key = sourcePath.Substring("$static:".Length);

            if (staticValues?.TryGetValue(key, out var staticValue) == true)
            {
                return staticValue;
            }

            _logger.LogWarning("Static value not found: {Key}", key);
            return null;
        }

        try
        {
            var value = ResolveFieldPath(source, sourcePath);
            return value?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving gRPC field path: {Path}", sourcePath);
            return null;
        }
    }

    /// <summary>
    /// Resolves a field path in a Protobuf message.
    /// </summary>
    private object? ResolveFieldPath(IMessage message, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var parts = path.Split('.');
        object? current = message;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            // Handle array/repeated field indexing: "items[0]"
            if (part.Contains('[') && part.Contains(']'))
            {
                var fieldName = part.Substring(0, part.IndexOf('['));
                var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                current = GetFieldValue(current, fieldName);

                if (current is System.Collections.IList list && int.TryParse(indexStr, out var index))
                {
                    if (index >= 0 && index < list.Count)
                    {
                        current = list[index];
                    }
                    else
                    {
                        _logger.LogWarning("Array index out of bounds: {Index} for field {Field}", index, fieldName);
                        return null;
                    }
                }
                else
                {
                    _logger.LogWarning("Field {Field} is not a repeated field or invalid index", fieldName);
                    return null;
                }
            }
            else
            {
                current = GetFieldValue(current, part);
            }
        }

        return current;
    }

    /// <summary>
    /// Gets a field value from a Protobuf message using reflection.
    /// </summary>
    private object? GetFieldValue(object obj, string fieldName)
    {
        if (obj is not IMessage message)
            return null;

        // Handle google.protobuf.Struct specially
        if (message is Struct structMessage)
        {
            if (structMessage.Fields.TryGetValue(fieldName, out var value))
            {
                return ConvertStructValue(value);
            }
            _logger.LogWarning("Field not found in Struct: {Field}", fieldName);
            return null;
        }

        // Handle google.protobuf.Any specially
        if (message is Any anyMessage)
        {
            // Any has TypeUrl and Value properties
            if (fieldName.Equals("TypeUrl", StringComparison.OrdinalIgnoreCase) ||
                fieldName.Equals("type_url", StringComparison.OrdinalIgnoreCase))
            {
                return anyMessage.TypeUrl;
            }
            if (fieldName.Equals("Value", StringComparison.OrdinalIgnoreCase))
            {
                return anyMessage.Value;
            }
        }

        var descriptor = message.Descriptor;
        var field = descriptor.FindFieldByName(fieldName);

        if (field == null)
        {
            // Try camelCase and PascalCase variations
            field = descriptor.FindFieldByName(ToCamelCase(fieldName))
                    ?? descriptor.FindFieldByName(ToPascalCase(fieldName));
        }

        if (field == null)
        {
            _logger.LogWarning("Field not found in message: {Field} in {MessageType}",
                fieldName, descriptor.Name);
            return null;
        }

        return field.Accessor.GetValue(message);
    }

    /// <summary>
    /// Converts a google.protobuf.Value to a C# object.
    /// </summary>
    private object? ConvertStructValue(Value value)
    {
        return value.KindCase switch
        {
            Value.KindOneofCase.NullValue => null,
            Value.KindOneofCase.NumberValue => value.NumberValue,
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            Value.KindOneofCase.StructValue => value.StructValue,
            Value.KindOneofCase.ListValue => value.ListValue,
            _ => null
        };
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
            return input;

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsUpper(input[0]))
            return input;

        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }
}
