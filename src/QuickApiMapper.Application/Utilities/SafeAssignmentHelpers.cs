using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Utilities;

/// <summary>
/// Demonstrates C# 14 null-conditional assignment operators.
/// The ?. and ?[] operators can now be used on the left-hand side of assignments.
/// </summary>
public static class SafeAssignmentHelpers
{
    /// <summary>
    /// C# 14 feature: Null-conditional assignment for dictionary values.
    /// Only assigns if the dictionary exists.
    /// </summary>
    /// <example>
    /// config.Metadata?["version"] = "2.0"; // No exception if Metadata is null
    /// </example>
    public static void SafelySetMetadata(dynamic config, string key, string value)
    {
        // C# 14: Null-conditional assignment - only assigns if config.Metadata is not null
        config.Metadata?[key] = value;
    }

    /// <summary>
    /// C# 14 feature: Null-conditional assignment for nested properties.
    /// Safely updates nested configuration without null checks.
    /// </summary>
    public static void UpdateNestedProperty(dynamic obj, string newValue)
    {
        // C# 14: Null-conditional member access on left-hand side
        // This is equivalent to: if (obj?.Settings != null) obj.Settings.Value = newValue;
        obj?.Settings.Value = newValue;
    }

    /// <summary>
    /// C# 14 feature: Compound assignment with null-conditional operators.
    /// Demonstrates += with null-conditional access.
    /// </summary>
    public static void IncrementCounter(dynamic state)
    {
        // C# 14: Null-conditional compound assignment
        // Only increments if state.Counters dictionary exists
        state.Counters?["requests"] += 1;
    }

    /// <summary>
    /// Safely updates integration static values using C# 14 null-conditional assignment.
    /// </summary>
    public static void UpdateStaticValue(IntegrationMapping integration, string key, string value)
    {
        // Note: Since IntegrationMapping uses IReadOnlyDictionary, we can't modify it directly
        // This is a pattern example - in real code, you'd need a mutable version
        // C# 14 would allow: integration.StaticValues?[key] = value; (if it was mutable)

        // Example with a wrapper class:
        var wrapper = new { MutableValues = integration.StaticValues as Dictionary<string, string> };
        wrapper.MutableValues?[key] = value;
    }

    /// <summary>
    /// C# 14 feature: Array element assignment with null-conditional indexer.
    /// </summary>
    public static void SafelyUpdateArrayElement<T>(T[]? array, int index, T value)
    {
        if (array == null || index >= array.Length)
            return;

        // C# 14: Null-conditional array assignment
        // This safely assigns only if array is not null
        array?[index] = value;
    }

    /// <summary>
    /// C# 14 feature: Null-conditional assignment in collection initializers.
    /// Updates list elements safely.
    /// </summary>
    public static void UpdateListElement<T>(List<T>? list, int index, T value)
    {
        if (list == null || index >= list.Count)
            return;

        // C# 14: Null-conditional list element assignment
        list?[index] = value;
    }
}
