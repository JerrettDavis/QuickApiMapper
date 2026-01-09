using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Extensions.gRPC.Writers;

/// <summary>
/// Destination writer for setting values in gRPC (Protobuf) messages.
/// Supports field paths and type conversion for Protobuf scalar types.
/// </summary>
public class GrpcDestinationWriter : IDestinationWriter<IMessage>
{
    private readonly ILogger<GrpcDestinationWriter> _logger;

    public GrpcDestinationWriter(ILogger<GrpcDestinationWriter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<string> SupportedTokens => new[] { "$grpc", "$protobuf" };

    public bool CanWrite(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
            return false;

        // Can write to gRPC-prefixed paths or plain field paths
        return destinationPath.StartsWith("$grpc:", StringComparison.OrdinalIgnoreCase) ||
               destinationPath.StartsWith("$protobuf:", StringComparison.OrdinalIgnoreCase) ||
               !destinationPath.StartsWith("$");
    }

    public bool Write(
        string destinationPath,
        string? value,
        IMessage destination)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            _logger.LogWarning("Destination path is null or empty");
            return false;
        }

        // Remove protocol prefix if present
        if (destinationPath.StartsWith("$grpc:", StringComparison.OrdinalIgnoreCase))
        {
            destinationPath = destinationPath.Substring("$grpc:".Length);
        }
        else if (destinationPath.StartsWith("$protobuf:", StringComparison.OrdinalIgnoreCase))
        {
            destinationPath = destinationPath.Substring("$protobuf:".Length);
        }

        try
        {
            SetFieldValue(destination, destinationPath, value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting gRPC field: {Path} = {Value}", destinationPath, value);
            return false;
        }
    }

    /// <summary>
    /// Sets a field value in a Protobuf message using a field path.
    /// </summary>
    private void SetFieldValue(IMessage message, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var parts = path.Split('.');
        var current = message;

        // Navigate to the parent message
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            var fieldValue = GetOrCreateFieldValue(current, part);

            if (fieldValue is IMessage nestedMessage)
            {
                current = nestedMessage;
            }
            else
            {
                _logger.LogWarning("Cannot navigate through non-message field: {Field}", part);
                return;
            }
        }

        // Set the final field value
        var fieldName = parts[^1];
        SetScalarFieldValue(current, fieldName, value);
    }

    /// <summary>
    /// Gets or creates a field value (for nested messages).
    /// </summary>
    private object? GetOrCreateFieldValue(IMessage message, string fieldName)
    {
        // Handle google.protobuf.Struct specially
        if (message is Struct structMessage)
        {
            if (structMessage.Fields.TryGetValue(fieldName, out var value))
            {
                // If it's a nested Struct, return it
                if (value.KindCase == Value.KindOneofCase.StructValue)
                {
                    return value.StructValue;
                }
                return value;
            }

            // Create a new nested Struct
            var newStruct = new Struct();
            structMessage.Fields[fieldName] = Value.ForStruct(newStruct);
            return newStruct;
        }

        var descriptor = message.Descriptor;
        var field = FindField(descriptor, fieldName);

        if (field == null)
        {
            _logger.LogWarning("Field not found: {Field} in {MessageType}", fieldName, descriptor.Name);
            return null;
        }

        var currentValue = field.Accessor.GetValue(message);

        // If the field is a message and it's null, create a new instance
        if (field.FieldType == FieldType.Message && currentValue == null)
        {
            var messageType = field.MessageType;
            var instance = Activator.CreateInstance(messageType.ClrType);
            field.Accessor.SetValue(message, instance);
            return instance;
        }

        return currentValue;
    }

    /// <summary>
    /// Sets a scalar field value with type conversion.
    /// </summary>
    private void SetScalarFieldValue(IMessage message, string fieldName, string? value)
    {
        // Handle google.protobuf.Struct specially
        if (message is Struct structMessage)
        {
            var structValue = ConvertToStructValue(value);
            structMessage.Fields[fieldName] = structValue;
            return;
        }

        var descriptor = message.Descriptor;
        var field = FindField(descriptor, fieldName);

        if (field == null)
        {
            _logger.LogWarning("Field not found: {Field} in {MessageType}", fieldName, descriptor.Name);
            return;
        }

        try
        {
            var convertedValue = ConvertValue(value, field);
            field.Accessor.SetValue(message, convertedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting value for field {Field}: {Value}", fieldName, value);
        }
    }

    /// <summary>
    /// Converts a string value to a google.protobuf.Value.
    /// </summary>
    private Value ConvertToStructValue(string? value)
    {
        if (value == null)
            return Value.ForNull();

        // Try to parse as number
        if (double.TryParse(value, out var numberValue))
            return Value.ForNumber(numberValue);

        // Try to parse as boolean
        if (bool.TryParse(value, out var boolValue))
            return Value.ForBool(boolValue);

        // Default to string
        return Value.ForString(value);
    }

    /// <summary>
    /// Converts a string value to the appropriate Protobuf field type.
    /// </summary>
    private object? ConvertValue(string? value, FieldDescriptor field)
    {
        if (value == null)
            return GetDefaultValue(field);

        return field.FieldType switch
        {
            FieldType.Double => double.Parse(value),
            FieldType.Float => float.Parse(value),
            FieldType.Int32 => int.Parse(value),
            FieldType.Int64 => long.Parse(value),
            FieldType.UInt32 => uint.Parse(value),
            FieldType.UInt64 => ulong.Parse(value),
            FieldType.SInt32 => int.Parse(value),
            FieldType.SInt64 => long.Parse(value),
            FieldType.Fixed32 => uint.Parse(value),
            FieldType.Fixed64 => ulong.Parse(value),
            FieldType.SFixed32 => int.Parse(value),
            FieldType.SFixed64 => long.Parse(value),
            FieldType.Bool => bool.Parse(value),
            FieldType.String => value,
            FieldType.Bytes => System.Text.Encoding.UTF8.GetBytes(value),
            FieldType.Enum => ParseEnum(field.EnumType, value),
            _ => value
        };
    }

    /// <summary>
    /// Parses an enum value from string (by name or number).
    /// </summary>
    private object ParseEnum(EnumDescriptor enumDescriptor, string value)
    {
        // Try parsing by name first
        var enumValue = enumDescriptor.FindValueByName(value);
        if (enumValue != null)
            return enumValue.Number;

        // Try parsing as number
        if (int.TryParse(value, out var number))
        {
            var enumValueByNumber = enumDescriptor.FindValueByNumber(number);
            if (enumValueByNumber != null)
                return number;
        }

        _logger.LogWarning("Invalid enum value: {Value} for {EnumType}", value, enumDescriptor.Name);
        return 0; // Default to first enum value
    }

    /// <summary>
    /// Gets the default value for a field type.
    /// </summary>
    private object? GetDefaultValue(FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.Double => 0.0,
            FieldType.Float => 0.0f,
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => 0,
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => 0L,
            FieldType.UInt32 or FieldType.Fixed32 => 0u,
            FieldType.UInt64 or FieldType.Fixed64 => 0uL,
            FieldType.Bool => false,
            FieldType.String => string.Empty,
            FieldType.Bytes => ByteString.Empty,
            _ => null
        };
    }

    /// <summary>
    /// Finds a field by name with case-insensitive fallback.
    /// </summary>
    private FieldDescriptor? FindField(MessageDescriptor descriptor, string fieldName)
    {
        var field = descriptor.FindFieldByName(fieldName);

        if (field == null)
        {
            // Try camelCase and PascalCase variations
            field = descriptor.FindFieldByName(ToCamelCase(fieldName))
                    ?? descriptor.FindFieldByName(ToPascalCase(fieldName));
        }

        return field;
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
