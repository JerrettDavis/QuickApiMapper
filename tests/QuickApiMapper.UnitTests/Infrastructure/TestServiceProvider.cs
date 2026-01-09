using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Application.Extensions;

namespace QuickApiMapper.UnitTests.Infrastructure;

/// <summary>
/// Provides a centralized, reusable test service provider for all unit tests.
/// Eliminates duplication and ensures consistent DI setup across tests.
/// </summary>
public static class TestServiceProvider
{
    private static readonly Lazy<IServiceProvider> ServiceProvider = new(CreateServiceProvider);

    /// <summary>
    /// Gets the shared test service provider instance.
    /// </summary>
    public static IServiceProvider Instance => ServiceProvider.Value;


    /// <summary>
    /// Creates a new isolated service provider for tests that need fresh state.
    /// </summary>
    /// <returns>
    /// A new instance of IServiceProvider with a fresh state.IDisposable
    /// </returns>    
#pragma warning disable IDISP004
#pragma warning disable IDISP005
    public static IServiceProvider CreateFresh() => CreateServiceProvider();
    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Add logging with test-friendly configuration
        services.AddLogging(builder =>
            builder.AddConsole()
                .AddDebug()
                .SetMinimumLevel(LogLevel.Information));

        // Use the new centralized service registration for consistent setup
        services.AddQuickApiMapper();



        return services.BuildServiceProvider();
#pragma warning restore IDISP004
#pragma warning restore IDISP005
    }
}