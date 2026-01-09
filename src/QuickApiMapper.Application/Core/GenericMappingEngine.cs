using Microsoft.Extensions.Logging;
using QuickApiMapper.Application.Transformers;
using QuickApiMapper.Contracts;
using ContractsMappingResult = QuickApiMapper.Contracts.MappingResult;

namespace QuickApiMapper.Application.Core;

/// <summary>
/// Generic mapping engine that can work with any source and destination types.
/// This replaces the hardcoded JObject/XDocument approach with a flexible, extensible system.
/// </summary>
/// <typeparam name="TSource">The source type to map from.</typeparam>
/// <typeparam name="TDestination">The destination type to map to.</typeparam>
public sealed class GenericMappingEngine<TSource, TDestination>(
    IEnumerable<ISourceResolver<TSource>> sourceResolvers,
    IEnumerable<IDestinationWriter<TDestination>> destinationWriters,
    IEnumerable<ISourceResolver<IReadOnlyDictionary<string, string>>> staticResolvers,
    ITransformerRegistry transformerRegistry,
    BehaviorPipeline behaviorPipeline,
    ILogger<GenericMappingEngine<TSource, TDestination>> logger
) : IMappingEngine<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    /// <summary>
    /// Applies field mappings to transform the source into the destination shape.
    /// </summary>
    public async Task<ContractsMappingResult> ApplyMappingAsync(
        IEnumerable<FieldMapping> mappings,
        TSource source,
        TDestination destination,
        IReadOnlyDictionary<string, string>? statics = null,
        IReadOnlyDictionary<string, string>? globalStatics = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        var context = new MappingContext<TSource, TDestination>
        {
            Mappings = mappings,
            TypedSource = source,
            TypedDestination = destination,
            Statics = statics,
            GlobalStatics = globalStatics,
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)),
            CancellationToken = cancellationToken
        };

        return await behaviorPipeline.ExecuteAsync(context, ExecuteCoreMappingLogic);
    }
    
    
    private record MappedField(
        string Source,
        string Destination,
        string? Value,
        IEnumerable<string> Transformers);

    /// <summary>
    /// Core mapping logic that executes the actual field mappings.
    /// </summary>
    private Task<ContractsMappingResult> ExecuteCoreMappingLogic(MappingContext context)
    {
        try
        {
            // Cast to generic context for type safety
            var typedContext = (MappingContext<TSource, TDestination>)context;

            logger.LogDebug("Starting core mapping logic with {MappingCount} mappings",
                context.Mappings.Count());

            // Merge global and integration-specific static values
            var mergedStatics = MergeStaticValues(context.GlobalStatics, context.Statics);

            // Process each field mapping
            var processedMappings = 0;
            var successfulMappings = 0;
            

            foreach (var field in context.Mappings)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                processedMappings++;

                try
                {
                    if (ProcessFieldMapping(field, typedContext.TypedSource, typedContext.TypedDestination, mergedStatics))
                        successfulMappings++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process field mapping: {Source} -> {Destination}",
                        field.Source, field.Destination);
                    throw;
                }
            }

            logger.LogInformation("Processed {Total} mappings, {Successful} successful",
                processedMappings, successfulMappings);

            return Task.FromResult(ContractsMappingResult.Success());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Core mapping logic failed");
            return Task.FromResult(ContractsMappingResult.Failure("Core mapping logic failed", ex));
        }
    }

    /// <summary>
    /// Processes a single field mapping.
    /// </summary>
    private bool ProcessFieldMapping(
        FieldMapping field,
        TSource source,
        TDestination destination,
        IReadOnlyDictionary<string, string> mergedStatics)
    {
        // Skip mappings without destination (used for schema generation only)
        if (string.IsNullOrEmpty(field.Destination))
        {
            logger.LogDebug("Skipping field mapping without destination: {Source}", field.Source);
            return false;
        }

        // Resolve the source value
        var rawValue = ResolveSourceValue(field.Source, source, mergedStatics);

        // Apply transformations
        var transformedValue = transformerRegistry.ApplyAll(rawValue, field.Transformers);

        // Write to destination
        return WriteToDestination(field.Destination, transformedValue, destination);
    }

    /// <summary>
    /// Resolves a source value using the appropriate resolver.
    /// </summary>
    private string? ResolveSourceValue(
        string sourcePath,
        TSource source,
        IReadOnlyDictionary<string, string> mergedStatics)
    {
        // First try static resolvers
        foreach (var staticResolver in staticResolvers)
        {
            if (staticResolver.CanResolve(sourcePath))
            {
                var staticValue = staticResolver.Resolve(sourcePath, mergedStatics, mergedStatics);
                if (staticValue != null)
                {
                    return staticValue;
                }
            }
        }

        // Then try source-specific resolvers
        foreach (var sourceResolver in sourceResolvers)
        {
            if (sourceResolver.CanResolve(sourcePath))
            {
                var sourceValue = sourceResolver.Resolve(sourcePath, source, mergedStatics);
                if (sourceValue != null)
                {
                    return sourceValue;
                }
            }
        }

        logger.LogWarning("No resolver found for source path: {SourcePath}", sourcePath);
        return null;
    }

    /// <summary>
    /// Writes a value to the destination using the appropriate writer.
    /// </summary>
    private bool WriteToDestination(string destinationPath, string? value, TDestination destination)
    {
        var writer = destinationWriters.FirstOrDefault(w => w.CanWrite(destinationPath));
        if (writer == null)
        {
            logger.LogWarning("No writer found for destination path: {Destination}", destinationPath);
            return false;
        }

        try
        {
            var success = writer.Write(destinationPath, value, destination);
            if (success)
            {
                logger.LogDebug("Successfully mapped: {Destination} = {Value}", destinationPath, value);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write to destination: {Destination}", destinationPath);
            return false;
        }
    }

    /// <summary>
    /// Merges global and integration-specific static values.
    /// </summary>
    private static IReadOnlyDictionary<string, string> MergeStaticValues(
        IReadOnlyDictionary<string, string>? globalStatics,
        IReadOnlyDictionary<string, string>? statics)
        => (globalStatics ?? Enumerable.Empty<KeyValuePair<string, string>>())
            .Concat(statics ?? Enumerable.Empty<KeyValuePair<string, string>>())
            .GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
}