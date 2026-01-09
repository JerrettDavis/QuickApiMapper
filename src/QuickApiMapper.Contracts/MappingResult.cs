namespace QuickApiMapper.Contracts;

/// <summary>
/// Represents the result of a mapping operation.
/// </summary>
public sealed class MappingResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    
    public static MappingResult Success() => new() { IsSuccess = true };
    public static MappingResult Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}