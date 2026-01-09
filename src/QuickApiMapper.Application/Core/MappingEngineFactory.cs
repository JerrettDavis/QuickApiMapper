using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Core;

/// <summary>
/// Factory for creating mapping engines for specific source/destination type combinations.
/// </summary>
public sealed class MappingEngineFactory(IServiceProvider serviceProvider, ILogger<MappingEngineFactory> logger) : IMappingEngineFactory
{
    /// <summary>
    /// Creates a mapping engine for the specified source and destination types.
    /// </summary>
    public IMappingEngine<TSource, TDestination> CreateEngine<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        logger.LogDebug("Creating mapping engine for {SourceType} -> {DestinationType}",
            typeof(TSource).Name, typeof(TDestination).Name);

        return serviceProvider.GetRequiredService<GenericMappingEngine<TSource, TDestination>>();
    }
}