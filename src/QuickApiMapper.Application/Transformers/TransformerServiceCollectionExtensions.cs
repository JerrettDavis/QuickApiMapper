using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Transformers;

public static class TransformerServiceCollectionExtensions
{
    public static IServiceCollection AddTransformers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        assemblies = assemblies is { Length: > 0 }
            ? assemblies
            : [Assembly.GetExecutingAssembly()];

        var transformerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(ITransformer).IsAssignableFrom(t) &&
                t is { IsInterface: false, IsAbstract: false })
            .ToList();

        foreach (var type in transformerTypes) services.AddSingleton(typeof(ITransformer), type);

        // The registry depends on all the above, so it can be constructed after DI resolves all ITransformer instances
        services.AddSingleton<ITransformerRegistry, TransformerRegistry>();

        return services;
    }

    public static IServiceCollection AddTransformersFromDirectory(
        this IServiceCollection services,
        string transformersDirectory = "Transformers")
    {
        // Load assemblies from the transformers directory
        var externalAssemblies = LoadTransformerAssemblies(transformersDirectory);
        
        // If we have external assemblies, add them to the existing transformer collection
        if (externalAssemblies.Count > 0)
        {
            var transformerTypes = externalAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(ITransformer).IsAssignableFrom(t) &&
                    t is { IsInterface: false, IsAbstract: false })
                .ToList();

            foreach (var type in transformerTypes) 
            {
                services.AddSingleton(typeof(ITransformer), type);
            }
        }

        // DON'T register the transformer registry here - it should be registered 
        // once after all transformers are loaded
        return services;
    }

    private static List<Assembly> LoadTransformerAssemblies(string directory)
    {
        var assemblies = new List<Assembly>();

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            return assemblies;
        }

        var dllFiles = Directory.GetFiles(directory, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);

                // Check if this assembly contains any transformer implementations
                var hasTransformers = assembly.GetTypes()
                    .Any(t => typeof(ITransformer).IsAssignableFrom(t) &&
                              !t.IsInterface && !t.IsAbstract);

                if (hasTransformers)
                {
                    assemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue loading other assemblies
                Console.WriteLine($"Failed to load assembly {dllFile}: {ex.Message}");
            }
        }

        return assemblies;
    }
}