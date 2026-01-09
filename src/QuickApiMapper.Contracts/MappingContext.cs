using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace QuickApiMapper.Contracts;

/// <summary>
/// Represents the context passed to behaviors during mapping execution.
/// </summary>
public class MappingContext
{
    public required IEnumerable<FieldMapping> Mappings { get; init; }
    
    public object? Source { get; init; }
    public object? Destination { get; init; }
    
    // Legacy properties for backward compatibility - will be removed in future versions
    [Obsolete("Use Source property with generic types instead")]
    public JObject? Json { get; init; }
    [Obsolete("Use Source property with generic types instead")]
    public XDocument? Xml { get; init; }
    
    public IReadOnlyDictionary<string, string>? Statics { get; init; }
    public IReadOnlyDictionary<string, string>? GlobalStatics { get; init; }
    public IServiceProvider ServiceProvider { get; init; } = default!;
    public CancellationToken CancellationToken { get; init; } = default;
    
    /// <summary>
    /// Custom properties that can be set by behaviors and shared across the pipeline.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Generic mapping context that provides strongly-typed access to source and destination objects.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
public sealed class MappingContext<TSource, TDestination> : MappingContext
    where TSource : class
    where TDestination : class
{
    public required TSource TypedSource { get; init; }
    public required TDestination TypedDestination { get; init; }
    
    public MappingContext()
    {
        // Set the base Source and Destination properties for backward compatibility
        Source = TypedSource;
        Destination = TypedDestination;
    }
}