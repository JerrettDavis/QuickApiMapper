namespace QuickApiMapper.Contracts;

/// <summary>
/// Generic destination writer that can work with any destination type.
/// </summary>
/// <typeparam name="TDestination">The destination type to write values to.</typeparam>
public interface IDestinationWriter<in TDestination> where TDestination : class
{
    /// <summary>
    /// The supported token prefixes that this writer can handle.
    /// </summary>
    IReadOnlyList<string> SupportedTokens { get; }

    /// <summary>
    /// Determines if this writer can handle the given destination path.
    /// </summary>
    /// <param name="destinationPath">The destination path to check.</param>
    /// <returns>True if this writer can handle the path.</returns>
    bool CanWrite(string destinationPath);

    /// <summary>
    /// Writes a value to the destination using the specified path.
    /// </summary>
    /// <param name="destinationPath">The path to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="destination">The destination object to write to.</param>
    /// <returns>True if the write was successful.</returns>
    bool Write(string destinationPath, string? value, TDestination destination);
}

