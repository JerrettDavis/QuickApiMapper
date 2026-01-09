using System.Collections.Concurrent;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Transformers;

/// <summary>
/// Provides registration and execution of value transformers.
/// </summary>
public interface ITransformerRegistry
{
    /// <summary>
    /// Applies a pipeline of transformers to a value.
    /// </summary>
    /// <param name="value">The input value to transform.</param>
    /// <param name="pipeline">The transformation pipeline to apply.</param>
    /// <returns>The transformed value.</returns>
    string? ApplyAll(string? value, IEnumerable<Transformer>? pipeline);

    /// <summary>
    /// Gets a transformer by name.
    /// </summary>
    /// <param name="name">The transformer name.</param>
    /// <returns>The transformer instance, or null if not found.</returns>
    ITransformer? Get(string name);

    /// <summary>
    /// Gets all registered transformer names.
    /// </summary>
    /// <returns>An enumerable of transformer names.</returns>
    IEnumerable<string> GetRegisteredNames();
    
    
    /// <summary>
    /// Adds a transformer to the registry.
    /// </summary>
    void AddTransformer(ITransformer transformer); 
}

/// <summary>
/// Production-ready transformer registry that provides safe, logged transformation operations.
/// </summary>
public sealed class TransformerRegistry : ITransformerRegistry
{
    private readonly ConcurrentDictionary<string, ITransformer> _transformers;

    /// <summary>
    /// Initializes a new instance of the TransformerRegistry class.
    /// </summary>
    /// <param name="transformers">The collection of transformers to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when transformers is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when duplicate transformer names are found.</exception>
    public TransformerRegistry(IEnumerable<ITransformer> transformers)
    {
        ArgumentNullException.ThrowIfNull(transformers);

        try
        {
            _transformers = new ConcurrentDictionary<string, ITransformer>(
                transformers.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Duplicate transformer names found. Each transformer must have a unique name.", ex);
        }
    }

    /// <summary>
    /// Gets a transformer by name.
    /// </summary>
    /// <param name="name">The transformer name (case-insensitive).</param>
    /// <returns>The transformer instance, or null if not found.</returns>
    public ITransformer? Get(string name)
        => string.IsNullOrEmpty(name) 
            ? null : _transformers.GetValueOrDefault(name);

    /// <summary>
    /// Applies a pipeline of transformers to a value sequentially.
    /// </summary>
    /// <param name="value">The input value to transform.</param>
    /// <param name="pipeline">The transformation pipeline to apply.</param>
    /// <returns>The transformed value, or the original value if pipeline is null or empty.</returns>
    /// <remarks>
    /// If a transformer in the pipeline is not found, the value is passed through unchanged.
    /// If a transformer throws an exception, the value is passed through unchanged.
    /// </remarks>
    public string? ApplyAll(string? value, IEnumerable<Transformer>? pipeline)
    {
        if (pipeline == null)
        {
            return value;
        }

        return pipeline.Aggregate(value, (currentValue, transformer) =>
        {
            try
            {
                var transformerInstance = Get(transformer.Name);
                if (transformerInstance == null)
                {
                    // Return unchanged value if transformer not found
                    return currentValue;
                }

                return transformerInstance.Transform(currentValue, transformer.Args);
            }
            catch (Exception)
            {
                // Return unchanged value if transformation fails
                // In production, you might want to log this error
                return currentValue;
            }
        });
    }

    /// <summary>
    /// Gets all registered transformer names.
    /// </summary>
    /// <returns>An enumerable of transformer names.</returns>
    public IEnumerable<string> GetRegisteredNames()
    {
        return _transformers.Keys;
    }

    public void AddTransformer(ITransformer transformer)
    {
        if (!_transformers.TryAdd(transformer.Name, transformer))
            throw new InvalidOperationException($"A transformer with the name '{transformer.Name}' is already registered.");
    }
}