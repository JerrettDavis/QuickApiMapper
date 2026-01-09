using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.SQLite;
using QuickApiMapper.Persistence.SQLite.Extensions;

namespace QuickApiMapper.IntegrationTests;

[TestFixture]
public class SqlitePersistenceTests
{
    private IServiceProvider? _serviceProvider;
    private string? _databasePath;

    [SetUp]
    public async Task Setup()
    {
        // Use in-memory SQLite database for each test
        _databasePath = Path.Combine(Path.GetTempPath(), $"quickapimapper_test_{Guid.NewGuid()}.db");

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSqlitePersistence($"Data Source={_databasePath}");

        _serviceProvider = services.BuildServiceProvider();

        // Apply migrations
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuickApiMapperSqliteDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose service provider to release all connections
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Clear all SQLite connection pools to release file handles
        SqliteConnection.ClearAllPools();

        // Force garbage collection to release any remaining handles
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Small delay to ensure file handles are released
        Thread.Sleep(100);

        if (_databasePath != null && File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
                // If still locked, try again after another GC
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(100);
                File.Delete(_databasePath);
            }
        }
    }

    [Test]
    public async Task AddAsync_Should_Create_Integration_Successfully()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "SqliteTest",
            Endpoint = "/api/sqlite",
            SourceType = "JSON",
            DestinationType = "XML",
            DestinationUrl = "https://example.com/xml"
        };

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("SqliteTest");
    }

    [Test]
    public async Task GetByIdAsync_Should_Return_Integration()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "GetByIdTest",
            Endpoint = "/api/getbyid",
            SourceType = "XML",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json"
        };

        var created = await repository.AddAsync(entity);

        // Act
        var result = await repository.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("GetByIdTest");
    }

    [Test]
    public async Task FieldMappings_Should_BePersisted_WithCorrectOrder()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "FieldMappingOrderTest",
            Endpoint = "/api/fieldorder",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json",
            FieldMappings =
            [
                new FieldMappingEntity { Source = "$.first", Destination = "$.result1", Order = 0 },
                new FieldMappingEntity { Source = "$.second", Destination = "$.result2", Order = 1 },
                new FieldMappingEntity { Source = "$.third", Destination = "$.result3", Order = 2 }
            ]
        };

        // Act
        await repository.AddAsync(entity);
        var result = await repository.GetByNameAsync("FieldMappingOrderTest");

        // Assert
        result.Should().NotBeNull();
        result!.FieldMappings.Should().HaveCount(3);
        result.FieldMappings.Should().BeInAscendingOrder(x => x.Order);
        result.FieldMappings[0].Source.Should().Be("$.first");
        result.FieldMappings[1].Source.Should().Be("$.second");
        result.FieldMappings[2].Source.Should().Be("$.third");
    }

    [Test]
    public async Task Transformers_Should_BePersisted_WithCorrectOrder()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "TransformerOrderTest",
            Endpoint = "/api/transformerorder",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json",
            FieldMappings =
            [
                new FieldMappingEntity
                {
                    Source = "$.name",
                    Destination = "$.fullName",
                    Order = 0,
                    Transformers =
                    [
                        new TransformerConfigEntity { Name = "Trim", Order = 0 },
                        new TransformerConfigEntity { Name = "ToUpper", Order = 1 },
                        new TransformerConfigEntity { Name = "Prefix", Order = 2, Arguments = "{\"prefix\":\"Mr. \"}" }
                    ]
                }
            ]
        };

        // Act
        await repository.AddAsync(entity);
        var result = await repository.GetByNameAsync("TransformerOrderTest");

        // Assert
        result.Should().NotBeNull();
        result!.FieldMappings[0].Transformers.Should().HaveCount(3);
        result.FieldMappings[0].Transformers.Should().BeInAscendingOrder(x => x.Order);
        result.FieldMappings[0].Transformers[0].Name.Should().Be("Trim");
        result.FieldMappings[0].Transformers[1].Name.Should().Be("ToUpper");
        result.FieldMappings[0].Transformers[2].Name.Should().Be("Prefix");
        result.FieldMappings[0].Transformers[2].Arguments.Should().Contain("prefix");
    }

    [Test]
    public async Task JsonSerialization_Should_Work_ForTransformerArguments()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var argumentsJson = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        var entity = new IntegrationMappingEntity
        {
            Name = "JsonSerializationTest",
            Endpoint = "/api/jsonserialization",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json",
            FieldMappings =
            [
                new FieldMappingEntity
                {
                    Source = "$.data",
                    Destination = "$.result",
                    Order = 0,
                    Transformers =
                    [
                        new TransformerConfigEntity
                        {
                            Name = "Custom",
                            Order = 0,
                            Arguments = argumentsJson
                        }
                    ]
                }
            ]
        };

        // Act
        await repository.AddAsync(entity);
        var result = await repository.GetByNameAsync("JsonSerializationTest");

        // Assert
        result.Should().NotBeNull();
        result!.FieldMappings[0].Transformers[0].Arguments.Should().Be(argumentsJson);
    }

    [Test]
    public async Task ConcurrentOperations_Should_NotCauseDataCorruption()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        // Act - Create multiple integrations concurrently
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var entity = new IntegrationMappingEntity
            {
                Name = $"ConcurrentTest{i}",
                Endpoint = $"/api/concurrent{i}",
                SourceType = "JSON",
                DestinationType = "JSON",
                DestinationUrl = $"https://example.com/test{i}"
            };
            return await repository.AddAsync(entity);
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyHaveUniqueItems(x => x.Id);
        results.Should().OnlyHaveUniqueItems(x => x.Name);

        var allIntegrations = await repository.GetAllActiveAsync();
        allIntegrations.Should().HaveCountGreaterOrEqualTo(10);
    }
}
