using System.Reflection;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Core;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Application.Providers;
using QuickApiMapper.Application.Resolvers;
using QuickApiMapper.Application.Transformers;
using QuickApiMapper.Application.Writers;
using QuickApiMapper.Contracts;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Application.Extensions;

/// <summary>
/// Provides extension methods for clean and discoverable service registration.
/// Eliminates boilerplate DI setup and ensures consistent component registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all QuickApiMapper core components and services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureLogging">Optional logging configuration.</param>
    /// <param name="transformerDirectory">Optional directory to load external transformer assemblies from.</param>
    /// <param name="behaviorDirectory">Optional directory to load external behavior assemblies from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQuickApiMapper(
        this IServiceCollection services,
        Action<ILoggingBuilder>? configureLogging = null,
        string? transformerDirectory = null,
        string? behaviorDirectory = null)
    {
        // Guard against duplicate registration - check if marker service already exists
        if (services.Any(s => s.ServiceType == typeof(QuickApiMapperMarker)))
        {
            return services;
        }

        // Add marker service to prevent duplicate registration
        services.AddSingleton<QuickApiMapperMarker>();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole().AddDebug();
            configureLogging?.Invoke(builder);
        });

        // Register core components
        services.AddQuickApiMapperCore();
        services.AddQuickApiMapperResolvers();
        services.AddQuickApiMapperWriters();
        services.AddQuickApiMapperDestinations();

        // Register transformers from embedded assemblies (without the registry)
        var assembliesToScan = GetQuickApiMapperAssemblies();
        RegisterTransformersFromAssemblies(services, assembliesToScan);

        // Also load transformers from external directory if specified
        if (!string.IsNullOrEmpty(transformerDirectory))
            services.AddTransformersFromDirectory(transformerDirectory);

        // Register the transformer registry ONCE at the end, after all transformers are loaded
        services.AddSingleton<ITransformerRegistry>(provider =>
        {
            var transformers = provider.GetServices<ITransformer>();
            return new TransformerRegistry(transformers);
        });

        // Register behavior pipeline system
        services.AddQuickApiMapperBehaviors(behaviorDirectory);

        return services;
    }

    /// <summary>
    /// Marker class to detect duplicate AddQuickApiMapper calls.
    /// </summary>
    private sealed class QuickApiMapperMarker { }
    
    /// <summary>
    /// Registers core mapping engine and related services.
    /// </summary>
    public static IServiceCollection AddQuickApiMapperCore(this IServiceCollection services)
    {
        // Register the new generic mapping system
        services.AddSingleton<IMappingEngineFactory, MappingEngineFactory>();
        
        // Register generic mapping engines for common type combinations
        services.AddSingleton<GenericMappingEngine<JObject, JObject>>();
        services.AddSingleton<GenericMappingEngine<XDocument, XDocument>>();
        services.AddSingleton<GenericMappingEngine<JObject, XDocument>>();
        services.AddSingleton<GenericMappingEngine<XDocument, JObject>>();
        
        // Register behavior pipeline
        services.AddSingleton<BehaviorPipeline>();
        
        // Register HTTP client for destination handlers
        services.AddHttpClient();
        
        return services;
    }
    
    /// <summary>
    /// Registers all source resolvers.
    /// </summary>
    public static IServiceCollection AddQuickApiMapperResolvers(this IServiceCollection services)
    {
        services.AddSingleton<ISourceResolver<IReadOnlyDictionary<string, string>>, StaticSourceResolver>();
        services.AddSingleton<ISourceResolver<JObject>, JsonSourceResolver>();
        services.AddSingleton<ISourceResolver<XDocument>, XmlSourceResolver>();
        return services;
    }
    
    /// <summary>
    /// Registers all destination writers.
    /// </summary>
    public static IServiceCollection AddQuickApiMapperWriters(this IServiceCollection services)
    {
        // Register the new generic destination writers
        services.AddSingleton<IDestinationWriter<XDocument>, XmlDestinationWriter>();
        services.AddSingleton<IDestinationWriter<JObject>, JsonDestinationWriter>();
        return services;
    }
    
    /// <summary>
    /// Registers all destination handlers.
    /// </summary>
    public static IServiceCollection AddQuickApiMapperDestinations(this IServiceCollection services)
    {
        services.AddSingleton<IDestinationHandler, JsonDestinationHandler>();
        services.AddSingleton<IDestinationHandler, SoapDestinationHandler>();
        return services;
    }
    
    /// <summary>
    /// Automatically discovers and registers all transformers from specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan. If null, scans all QuickApiMapper assemblies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQuickApiMapperTransformers(
        this IServiceCollection services,
        params Assembly[]? assemblies)
    {
        // Use the existing transformer loading logic instead of duplicating it
        var assembliesToScan = assemblies?.Length > 0 
            ? assemblies 
            : GetQuickApiMapperAssemblies();
        
        // Use the existing AddTransformers method that handles registration properly
        return services.AddTransformers(assembliesToScan);
    }
    
    /// <summary>
    /// Adds a specific transformer type.
    /// </summary>
    /// <typeparam name="T">The transformer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTransformer<T>(this IServiceCollection services)
        where T : class, ITransformer
    {
        services.AddSingleton<ITransformer, T>();
        return services;
    }
    
    /// <summary>
    /// Registers transformers from the specified assemblies without registering the registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for transformers.</param>
    private static void RegisterTransformersFromAssemblies(IServiceCollection services, Assembly[] assemblies)
    {
        var transformerType = typeof(ITransformer);
        var registeredTypes = new HashSet<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var transformers = assembly.GetTypes()
                    .Where(t => transformerType.IsAssignableFrom(t) &&
                               !t.IsInterface &&
                               !t.IsAbstract)
                    .ToList();

                foreach (var transformer in transformers)
                {
                    // Check both local HashSet and ServiceCollection to prevent duplicates
                    if (registeredTypes.Add(transformer) &&
                        !services.Any(s => s.ServiceType == transformerType && s.ImplementationType == transformer))
                    {
                        services.AddSingleton(transformerType, transformer);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Log which types couldn't be loaded but continue
                var loadableTypes = ex.Types.Where(t => t != null).ToList();
                var transformers = loadableTypes
                    .Where(t => transformerType.IsAssignableFrom(t) &&
                                t is { IsInterface: false, IsAbstract: false })
                    .Cast<Type>()
                    .ToList();

                foreach (var transformer in transformers)
                {
                    // Check both local HashSet and ServiceCollection to prevent duplicates
                    if (registeredTypes.Add(transformer) &&
                        !services.Any(s => s.ServiceType == transformerType && s.ImplementationType == transformer))
                    {
                        services.AddSingleton(transformerType, transformer);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Registers the behavior pipeline system and automatically discovers behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="behaviorDirectory">Optional directory to load external behavior assemblies from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQuickApiMapperBehaviors(
        this IServiceCollection services,
        string? behaviorDirectory = null)
    {
        if (!string.IsNullOrEmpty(behaviorDirectory))
            services.AddBehaviorsFromDirectory(behaviorDirectory);
        
        return services;
    }
    
    /// <summary>
    /// Loads behaviors from a specified directory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="directory">The directory to scan for behavior assemblies.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBehaviorsFromDirectory(
        this IServiceCollection services,
        string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Behavior directory not found: {directory}");
        }
        
        var assemblyFiles = Directory.GetFiles(directory, "*.dll");
        var assemblies = new List<Assembly>();
        
        foreach (var file in assemblyFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                assemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                // Log error but continue with other assemblies
                Console.WriteLine($"Failed to load behavior assembly {file}: {ex.Message}");
            }
        }
        
        AddBehaviorsFromAssemblies(services, [.. assemblies]);
        return services;
    }
    
    /// <summary>
    /// Adds a specific behavior type.
    /// </summary>
    /// <typeparam name="T">The behavior type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBehavior<T>(this IServiceCollection services)
        where T : class, IWholeRunBehavior, IPostRunBehavior, IPreRunBehavior
    {
        // Register based on the specific behavior interface
        if (typeof(IPreRunBehavior).IsAssignableFrom(typeof(T)))
        {
            services.AddSingleton<IPreRunBehavior, T>();
        }
        
        if (typeof(IPostRunBehavior).IsAssignableFrom(typeof(T)))
        {
            services.AddSingleton<IPostRunBehavior, T>();
        }
        
        if (typeof(IWholeRunBehavior).IsAssignableFrom(typeof(T)))
        {
            services.AddSingleton<IWholeRunBehavior, T>();
        }
        
        return services;
    }
    
    /// <summary>
    /// Registers behaviors from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for behaviors.</param>
    public static void AddBehaviorsFromAssemblies(this IServiceCollection services, Assembly[] assemblies)
    {
        var behaviorTypes = new[]
        {
            typeof(IPreRunBehavior),
            typeof(IPostRunBehavior),
            typeof(IWholeRunBehavior)
        };
        
        var registeredTypes = new HashSet<Type>();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var allTypes = assembly.GetTypes()
                    .Where(t => !t.IsInterface && !t.IsAbstract)
                    .ToList();
                
                foreach (var behaviorType in behaviorTypes)
                {
                    var behaviors = allTypes
                        .Where(t => behaviorType.IsAssignableFrom(t))
                        .ToList();
                    
                    foreach (var behavior in behaviors)
                    {
                        if (registeredTypes.Add(behavior))
                        {
                            services.AddSingleton(behaviorType, behavior);
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Log which types couldn't be loaded but continue
                var loadableTypes = ex.Types.Where(t => t != null).ToList();
                
                foreach (var behaviorType in behaviorTypes)
                {
                    var behaviors = loadableTypes
                        .Where(t => behaviorType.IsAssignableFrom(t) && 
                                   t is { IsInterface: false, IsAbstract: false })
                        .Cast<Type>()
                        .ToList();
                    
                    foreach (var behavior in behaviors)
                    {
                        if (registeredTypes.Add(behavior))
                        {
                            services.AddSingleton(behaviorType, behavior);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Gets all assemblies that contain QuickApiMapper components.
    /// </summary>
    private static Assembly[] GetQuickApiMapperAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.Contains("QuickApiMapper") == true &&
                       !a.FullName.Contains("Tests") &&  // Exclude test assemblies
                       !a.FullName.Contains("Test"))     // Exclude test assemblies (alternative naming)
            .ToArray();
    }

    // ============================================================================
    // Configuration Provider Extensions (Persistence Layer)
    // ============================================================================

    /// <summary>
    /// Adds file-based configuration provider (reads from appsettings.json).
    /// This is the default mode for backward compatibility.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFileBasedConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IIntegrationConfigurationProvider, FileBasedConfigurationProvider>();
        return services;
    }

    /// <summary>
    /// Adds database-backed configuration provider.
    /// Requires a persistence implementation (PostgreSQL or SQLite) to be registered first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IIntegrationConfigurationProvider, DatabaseConfigurationProvider>();
        return services;
    }

    /// <summary>
    /// Wraps the current configuration provider with caching.
    /// Should be called AFTER AddFileBasedConfiguration or AddDatabaseConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cacheExpiration">Cache expiration time. Default is 5 minutes.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCachedConfiguration(
        this IServiceCollection services,
        TimeSpan? cacheExpiration = null)
    {
        // Ensure memory cache is available
        services.AddMemoryCache();

        // Decorate the existing provider with caching
        services.Decorate<IIntegrationConfigurationProvider>((inner, sp) =>
            new CachedConfigurationProvider(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedConfigurationProvider>>(),
                cacheExpiration));

        return services;
    }

    /// <summary>
    /// Adds QuickApiMapper with persistence support.
    /// This is a convenience method that configures the provider based on options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure persistence options.</param>
    /// <param name="configureLogging">Optional logging configuration.</param>
    /// <param name="transformerDirectory">Optional directory to load external transformer assemblies from.</param>
    /// <param name="behaviorDirectory">Optional directory to load external behavior assemblies from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQuickApiMapperWithPersistence(
        this IServiceCollection services,
        Action<PersistenceOptions> configure,
        Action<ILoggingBuilder>? configureLogging = null,
        string? transformerDirectory = null,
        string? behaviorDirectory = null)
    {
        // Register core QuickApiMapper components
        services.AddQuickApiMapper(configureLogging, transformerDirectory, behaviorDirectory);

        // Configure persistence options
        var options = new PersistenceOptions();
        configure(options);

        // Register appropriate configuration provider
        if (options.UseDatabase)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionString must be provided when UseDatabase is true");
            }

            // Database provider is registered by the persistence project (PostgreSQL/SQLite)
            // Just register the DatabaseConfigurationProvider
            services.AddDatabaseConfiguration();
        }
        else
        {
            // Use file-based configuration (backward compatible)
            services.AddFileBasedConfiguration();
        }

        // Add caching if enabled
        if (options.EnableCaching)
        {
            services.AddCachedConfiguration(options.CacheExpiration);
        }

        return services;
    }
}

/// <summary>
/// Options for configuring persistence in QuickApiMapper.
/// </summary>
public class PersistenceOptions
{
    /// <summary>
    /// Whether to use database for configuration storage.
    /// If false, uses file-based configuration (appsettings.json).
    /// </summary>
    public bool UseDatabase { get; set; }

    /// <summary>
    /// Database connection string (required if UseDatabase is true).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether to enable caching for configuration.
    /// Recommended for production deployments.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time. Default is 5 minutes.
    /// </summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
