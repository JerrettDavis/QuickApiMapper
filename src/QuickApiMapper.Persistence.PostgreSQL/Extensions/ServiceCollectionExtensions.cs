using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.PostgreSQL.Repositories;

namespace QuickApiMapper.Persistence.PostgreSQL.Extensions;

/// <summary>
/// Service collection extensions for PostgreSQL persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PostgreSQL persistence services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configureOptions">Optional action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlPersistence(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        services.AddDbContext<QuickApiMapperDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(QuickApiMapperDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            configureOptions?.Invoke(options);
        });

        // Register repositories
        services.AddScoped<IIntegrationMappingRepository, IntegrationMappingRepository>();
        services.AddScoped<IGlobalToggleRepository, GlobalToggleRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, Repositories.UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task InitializeDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();
    }
}
