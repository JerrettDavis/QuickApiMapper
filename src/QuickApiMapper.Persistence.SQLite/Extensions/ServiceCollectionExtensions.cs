using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.SQLite.Repositories;

namespace QuickApiMapper.Persistence.SQLite.Extensions;

/// <summary>
/// Service collection extensions for SQLite persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SQLite persistence services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQLite connection string (e.g., "Data Source=quickapimapper.db").</param>
    /// <param name="configureOptions">Optional action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqlitePersistence(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        services.AddDbContext<QuickApiMapperSqliteDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(QuickApiMapperSqliteDbContext).Assembly.FullName);
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
        var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperSqliteDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();
    }
}
