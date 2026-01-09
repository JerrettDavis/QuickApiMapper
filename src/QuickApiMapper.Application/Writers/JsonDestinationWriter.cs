using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Writers;

/// <summary>
/// Writes values to JSON object destinations using JSONPath-style syntax.
/// Supports nested object creation and null value handling.
/// </summary>
public sealed class JsonDestinationWriter : IDestinationWriter<JObject>
{
    public IReadOnlyList<string> SupportedTokens => ["$."];

    /// <summary>
    /// Determines if this writer can handle the specified destination path.
    /// </summary>
    /// <param name="destPath">The destination path to check.</param>
    /// <returns>True if the path starts with "$.", false otherwise.</returns>
    public bool CanWrite(string destPath) => destPath.StartsWith("$.");

    /// <summary>
    /// Writes a value to the specified JSON path in the target object.
    /// </summary>
    /// <param name="destinationPath">The JSON path where to write the value (e.g., "$.user.name").</param>
    /// <param name="value">The value to write.</param>
    /// <param name="destination">The target JSON object.</param>
    /// <returns>True if the write operation was successful, false otherwise.</returns>
    public bool Write(string destinationPath, string? value, JObject destination)
    {
        if (string.IsNullOrEmpty(destinationPath) || !destinationPath.StartsWith("$."))
            return false;

        try
        {
            // Remove the "$." prefix to get the actual path
            var path = destinationPath[2..];
            
            // Handle root-level assignment
            if (string.IsNullOrEmpty(path))
            {
                return false; // Cannot write to root
            }

            // Split the path into parts
            var pathParts = path.Split('.');
            var current = destination;

            // Navigate/create the path up to the last part
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                var part = pathParts[i];
                
                if (current[part] == null)
                    current[part] = new JObject();
                
                current = (JObject)current[part]!;
            }

            // Set the final value
            var finalKey = pathParts[^1];
            current[finalKey] = value;
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}