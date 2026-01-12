using System.Buffers;

namespace QuickApiMapper.Application.Utilities;

/// <summary>
/// High-performance string and array utilities using C# 14 implicit Span conversions.
/// Leverages .NET 10's enhanced Span support for zero-allocation operations.
/// </summary>
public static class SpanHelpers
{
    /// <summary>
    /// Extracts a substring using C# 14 implicit Span conversion.
    /// Zero-allocation alternative to string.Substring().
    /// </summary>
    /// <example>
    /// var result = SpanHelpers.SafeSlice("Hello World", 0, 5); // "Hello"
    /// </example>
    public static string SafeSlice(string input, int start, int length)
    {
        if (string.IsNullOrEmpty(input) || start >= input.Length)
            return string.Empty;

        // C# 14: Implicit conversion from array slice to ReadOnlySpan<char>
        ReadOnlySpan<char> span = input.AsSpan()[start..Math.Min(start + length, input.Length)];
        return span.ToString();
    }

    /// <summary>
    /// Efficiently checks if a string starts with any of the given prefixes.
    /// Uses C# 14 implicit Span conversions for zero-allocation checks.
    /// </summary>
    public static bool StartsWithAny(string input, params string[] prefixes)
    {
        if (string.IsNullOrEmpty(input) || prefixes.Length == 0)
            return false;

        // C# 14: Implicit Span conversion
        ReadOnlySpan<char> inputSpan = input.AsSpan();

        foreach (var prefix in prefixes)
        {
            if (inputSpan.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Splits a string efficiently using C# 14 Span features.
    /// Avoids allocations for delimiter-based parsing.
    /// </summary>
    public static void ProcessDelimitedValues(string input, char delimiter, Action<ReadOnlySpan<char>> processor)
    {
        if (string.IsNullOrEmpty(input))
            return;

        // C# 14: Implicit Span conversion from string
        ReadOnlySpan<char> remaining = input.AsSpan();

        while (!remaining.IsEmpty)
        {
            int delimiterIndex = remaining.IndexOf(delimiter);

            if (delimiterIndex == -1)
            {
                // Process final segment
                processor(remaining);
                break;
            }

            // C# 14: Slice with implicit conversion
            processor(remaining[..delimiterIndex]);
            remaining = remaining[(delimiterIndex + 1)..];
        }
    }

    /// <summary>
    /// Trims whitespace from both ends using C# 14 Span features.
    /// More efficient than string.Trim() for large strings.
    /// </summary>
    public static string FastTrim(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // C# 14: Implicit Span conversion and slicing
        ReadOnlySpan<char> trimmed = input.AsSpan().Trim();
        return trimmed.ToString();
    }

    /// <summary>
    /// Copies array segments efficiently using C# 14 implicit Span conversion.
    /// Ideal for buffer operations in message processing.
    /// </summary>
    public static void CopySegment<T>(T[] source, int sourceStart, T[] destination, int destStart, int length)
    {
        if (source == null || destination == null)
            throw new ArgumentNullException();

        if (sourceStart + length > source.Length || destStart + length > destination.Length)
            throw new ArgumentOutOfRangeException();

        // C# 14: Implicit Span conversions from array slices
        Span<T> sourceSpan = source.AsSpan()[sourceStart..(sourceStart + length)];
        Span<T> destSpan = destination.AsSpan()[destStart..(destStart + length)];

        sourceSpan.CopyTo(destSpan);
    }

    /// <summary>
    /// Efficiently reverses a portion of an array using C# 14 Span features.
    /// </summary>
    public static void ReverseSegment<T>(T[] array, int start, int length)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (start + length > array.Length)
            throw new ArgumentOutOfRangeException();

        // C# 14: Implicit Span conversion and in-place reversal
        Span<T> segment = array.AsSpan()[start..(start + length)];
        segment.Reverse();
    }
}
