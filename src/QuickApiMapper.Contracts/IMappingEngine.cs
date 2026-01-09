namespace QuickApiMapper.Contracts;

/// <summary>
/// Generic mapping engine interface that can work with any source and destination types.
/// </summary>
/// <typeparam name="TSource">The source type to map from.</typeparam>
/// <typeparam name="TDestination">The destination type to map to.</typeparam>
public interface IMappingEngine<in TSource, in TDestination>
    where TSource : class
    where TDestination : class
{
    /// <summary>
    /// Applies field mappings to transform the source into the destination shape.
    /// </summary>
    /// <param name="mappings">The field mappings to apply.</param>
    /// <param name="source">The source object to map from.</param>
    /// <param name="destination">The destination object to map to.</param>
    /// <param name="statics">Static values for the mapping.</param>
    /// <param name="globalStatics">Global static values.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping result.</returns>
    Task<MappingResult> ApplyMappingAsync(
        IEnumerable<FieldMapping> mappings,
        TSource source,
        TDestination destination,
        IReadOnlyDictionary<string, string>? statics = null,
        IReadOnlyDictionary<string, string>? globalStatics = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating mapping engines for specific source/destination type combinations.
/// </summary>
public interface IMappingEngineFactory
{
    /// <summary>
    /// Creates a mapping engine for the specified source and destination types.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <returns>A mapping engine instance.</returns>
    IMappingEngine<TSource, TDestination> CreateEngine<TSource, TDestination>()
        where TSource : class
        where TDestination : class;
}