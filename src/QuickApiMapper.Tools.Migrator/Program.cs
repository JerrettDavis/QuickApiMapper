using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.PostgreSQL.Extensions;
using QuickApiMapper.Persistence.SQLite.Extensions;

namespace QuickApiMapper.Tools.Migrator;

/// <summary>
/// Migration tool for converting appsettings.json configurations to database storage.
///
/// Usage:
///   QuickApiMapper.Tools.Migrator --source appsettings.json --db-type postgresql --connection "Host=localhost;Database=quickapimapper;Username=postgres;Password=postgres"
///   QuickApiMapper.Tools.Migrator --source appsettings.json --db-type sqlite --connection "Data Source=quickapimapper.db"
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);

        if (options == null)
        {
            PrintUsage();
            return 1;
        }

        using var host = CreateHost(options);

        using (var scope = host.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

            try
            {
                logger.LogInformation("Starting migration from {Source} to {DbType} database",
                    options.SourceFile, options.DatabaseType);

                // Read configuration from file
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(options.SourceFile) ?? Directory.GetCurrentDirectory())
                    .AddJsonFile(Path.GetFileName(options.SourceFile), optional: false, reloadOnChange: false)
                    .Build();

                var apiMappingConfig = configuration
                    .GetSection("ApiMapping")
                    .Get<ApiMappingConfig>();

                if (apiMappingConfig?.Mappings == null || !apiMappingConfig.Mappings.Any())
                {
                    logger.LogWarning("No integrations found in configuration file");
                    return 0;
                }

                logger.LogInformation("Found {Count} integrations to migrate", apiMappingConfig.Mappings.Count);

                // Migrate each integration
                foreach (var integration in apiMappingConfig.Mappings)
                {
                    logger.LogInformation("Migrating integration: {Name}", integration.Name);

                    // Check if integration already exists
                    var existing = await repository.GetByNameAsync(integration.Name);
                    if (existing != null && !options.Overwrite)
                    {
                        logger.LogWarning("Integration {Name} already exists. Use --overwrite to replace it.", integration.Name);
                        continue;
                    }

                    var entity = MapToEntity(integration, apiMappingConfig.StaticValues, apiMappingConfig.Namespaces);

                    if (existing != null)
                    {
                        entity.Id = existing.Id;
                        await repository.UpdateAsync(entity);
                        logger.LogInformation("Updated integration: {Name}", integration.Name);
                    }
                    else
                    {
                        await repository.AddAsync(entity);
                        logger.LogInformation("Created integration: {Name}", integration.Name);
                    }
                }

                logger.LogInformation("Migration completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed");
                return 1;
            }
        }
    }

    private static MigrationOptions? ParseArguments(string[] args)
    {
        var options = new MigrationOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--source":
                case "-s":
                    if (i + 1 < args.Length)
                        options.SourceFile = args[++i];
                    break;

                case "--db-type":
                case "-t":
                    if (i + 1 < args.Length)
                        options.DatabaseType = args[++i];
                    break;

                case "--connection":
                case "-c":
                    if (i + 1 < args.Length)
                        options.ConnectionString = args[++i];
                    break;

                case "--overwrite":
                case "-o":
                    options.Overwrite = true;
                    break;

                case "--help":
                case "-h":
                    return null;
            }
        }

        // Validate required options
        if (string.IsNullOrEmpty(options.SourceFile) ||
            string.IsNullOrEmpty(options.DatabaseType) ||
            string.IsNullOrEmpty(options.ConnectionString))
        {
            return null;
        }

        if (!File.Exists(options.SourceFile))
        {
            Console.WriteLine($"Error: Source file not found: {options.SourceFile}");
            return null;
        }

        if (options.DatabaseType.ToLowerInvariant() != "postgresql" &&
            options.DatabaseType.ToLowerInvariant() != "sqlite")
        {
            Console.WriteLine($"Error: Unsupported database type: {options.DatabaseType}. Use 'postgresql' or 'sqlite'.");
            return null;
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("QuickApiMapper Configuration Migrator");
        Console.WriteLine();
        Console.WriteLine("Migrates integration configurations from appsettings.json to database storage.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  QuickApiMapper.Tools.Migrator --source <file> --db-type <type> --connection <connection-string> [--overwrite]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --source, -s       Path to appsettings.json file (required)");
        Console.WriteLine("  --db-type, -t      Database type: 'postgresql' or 'sqlite' (required)");
        Console.WriteLine("  --connection, -c   Database connection string (required)");
        Console.WriteLine("  --overwrite, -o    Overwrite existing integrations");
        Console.WriteLine("  --help, -h         Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Migrate to PostgreSQL");
        Console.WriteLine("  QuickApiMapper.Tools.Migrator \\");
        Console.WriteLine("    --source appsettings.json \\");
        Console.WriteLine("    --db-type postgresql \\");
        Console.WriteLine("    --connection \"Host=localhost;Database=quickapimapper;Username=postgres;Password=postgres\"");
        Console.WriteLine();
        Console.WriteLine("  # Migrate to SQLite");
        Console.WriteLine("  QuickApiMapper.Tools.Migrator \\");
        Console.WriteLine("    --source appsettings.json \\");
        Console.WriteLine("    --db-type sqlite \\");
        Console.WriteLine("    --connection \"Data Source=quickapimapper.db\"");
    }

    private static IHost CreateHost(MigrationOptions options)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                // Register appropriate persistence provider
                if (options.DatabaseType.ToLowerInvariant() == "postgresql")
                {
                    services.AddPostgreSqlPersistence(options.ConnectionString);
                }
                else if (options.DatabaseType.ToLowerInvariant() == "sqlite")
                {
                    services.AddSqlitePersistence(options.ConnectionString);
                }
            });

        return builder.Build();
    }

    private static IntegrationMappingEntity MapToEntity(
        IntegrationMapping integration,
        IReadOnlyDictionary<string, string>? globalStaticValues,
        IReadOnlyDictionary<string, string>? namespaces)
    {
        var entity = new IntegrationMappingEntity
        {
            Id = Guid.NewGuid(),
            Name = integration.Name,
            Endpoint = integration.Endpoint,
            SourceType = integration.SourceType,
            DestinationType = integration.DestinationType,
            DestinationUrl = integration.DestinationUrl,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Map field mappings
        if (integration.Mapping != null)
        {
            var order = 0;
            foreach (var fieldMapping in integration.Mapping)
            {
                var fieldMappingEntity = new FieldMappingEntity
                {
                    Id = Guid.NewGuid(),
                    IntegrationMappingId = entity.Id,
                    Source = fieldMapping.Source,
                    Destination = fieldMapping.Destination,
                    Order = order++
                };

                // Map transformers
                if (fieldMapping.Transformers != null)
                {
                    var transformerOrder = 0;
                    foreach (var transformer in fieldMapping.Transformers)
                    {
                        var transformerEntity = new TransformerConfigEntity
                        {
                            Id = Guid.NewGuid(),
                            FieldMappingId = fieldMappingEntity.Id,
                            Name = transformer.Name,
                            Order = transformerOrder++,
                            Arguments = transformer.Args != null
                                ? System.Text.Json.JsonSerializer.Serialize(transformer.Args)
                                : null
                        };
                        fieldMappingEntity.Transformers.Add(transformerEntity);
                    }
                }

                entity.FieldMappings.Add(fieldMappingEntity);
            }
        }

        // Map static values (integration-specific)
        if (integration.StaticValues != null)
        {
            foreach (var kvp in integration.StaticValues)
            {
                entity.StaticValues.Add(new StaticValueEntity
                {
                    Id = Guid.NewGuid(),
                    IntegrationMappingId = entity.Id,
                    Key = kvp.Key,
                    Value = kvp.Value,
                    IsGlobal = false
                });
            }
        }

        // Map global static values
        if (globalStaticValues != null)
        {
            foreach (var kvp in globalStaticValues)
            {
                // Only add if not already present in integration-specific values
                if (integration.StaticValues?.ContainsKey(kvp.Key) != true)
                {
                    entity.StaticValues.Add(new StaticValueEntity
                    {
                        Id = Guid.NewGuid(),
                        IntegrationMappingId = entity.Id,
                        Key = kvp.Key,
                        Value = kvp.Value,
                        IsGlobal = true
                    });
                }
            }
        }

        // Map SOAP configuration
        if (integration.SoapConfig != null)
        {
            var soapConfig = new SoapConfigEntity
            {
                Id = Guid.NewGuid(),
                IntegrationMappingId = entity.Id,
                BodyWrapperFieldXPath = integration.SoapConfig.BodyWrapperFieldXPath
            };

            // Map header fields
            if (integration.SoapConfig.HeaderFields != null)
            {
                var order = 0;
                foreach (var field in integration.SoapConfig.HeaderFields)
                {
                    soapConfig.Fields.Add(new SoapFieldEntity
                    {
                        Id = Guid.NewGuid(),
                        SoapConfigId = soapConfig.Id,
                        FieldType = "Header",
                        XPath = field.XPath,
                        Source = field.Source,
                        Namespace = field.Namespace,
                        Prefix = field.Prefix,
                        Attributes = field.Attributes != null
                            ? System.Text.Json.JsonSerializer.Serialize(field.Attributes)
                            : null,
                        Order = order++
                    });
                }
            }

            // Map body fields
            if (integration.SoapConfig.BodyFields != null)
            {
                var order = 0;
                foreach (var field in integration.SoapConfig.BodyFields)
                {
                    soapConfig.Fields.Add(new SoapFieldEntity
                    {
                        Id = Guid.NewGuid(),
                        SoapConfigId = soapConfig.Id,
                        FieldType = "Body",
                        XPath = field.XPath,
                        Source = field.Source,
                        Namespace = field.Namespace,
                        Prefix = field.Prefix,
                        Attributes = field.Attributes != null
                            ? System.Text.Json.JsonSerializer.Serialize(field.Attributes)
                            : null,
                        Order = order++
                    });
                }
            }

            entity.SoapConfig = soapConfig;
        }

        return entity;
    }
}

class MigrationOptions
{
    public string SourceFile { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool Overwrite { get; set; }
}
