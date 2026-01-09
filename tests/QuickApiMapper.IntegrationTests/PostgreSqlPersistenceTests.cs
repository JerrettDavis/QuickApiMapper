using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.PostgreSQL;
using QuickApiMapper.Persistence.PostgreSQL.Extensions;
using Testcontainers.PostgreSql;

namespace QuickApiMapper.IntegrationTests;

[TestFixture]
public class PostgreSqlPersistenceTests
{
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("quickapimapper_test")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        await _postgresContainer.StartAsync();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddPostgreSqlPersistence(_postgresContainer.GetConnectionString());

        _serviceProvider = services.BuildServiceProvider();

        // Apply migrations
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuickApiMapperDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Test]
    public async Task AddAsync_Should_Create_Integration_With_AllRelatedEntities()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "TestIntegration",
            Endpoint = "/api/test",
            SourceType = "JSON",
            DestinationType = "SOAP",
            DestinationUrl = "https://example.com/soap",
            FieldMappings =
            [
                new FieldMappingEntity
                {
                    Source = "$.customer.name",
                    Destination = "//CustomerName",
                    Order = 0,
                    Transformers =
                    [
                        new TransformerConfigEntity
                        {
                            Name = "ToUpper",
                            Order = 0
                        }
                    ]
                }
            ],
            StaticValues =
            [
                new StaticValueEntity
                {
                    Key = "ApiKey",
                    Value = "test_key",
                    IsGlobal = false
                }
            ]
        };

        // Act
        var result = await repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("TestIntegration");
        result.FieldMappings.Should().HaveCount(1);
        result.FieldMappings[0].Transformers.Should().HaveCount(1);
        result.StaticValues.Should().HaveCount(1);
    }

    [Test]
    public async Task GetByNameAsync_Should_Return_Integration_WithEagerLoadedRelations()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "EagerLoadTest",
            Endpoint = "/api/eagerload",
            SourceType = "XML",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json"
        };

        await repository.AddAsync(entity);

        // Act
        var result = await repository.GetByNameAsync("EagerLoadTest");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("EagerLoadTest");
        result.FieldMappings.Should().NotBeNull();
        result.StaticValues.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateAsync_Should_UpdateFields_And_IncrementVersion()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "UpdateTest",
            Endpoint = "/api/update",
            SourceType = "JSON",
            DestinationType = "XML",
            DestinationUrl = "https://example.com/xml",
            Version = 1
        };

        var created = await repository.AddAsync(entity);
        var originalVersion = created.Version;

        // Act
        created.DestinationUrl = "https://newurl.com/xml";
        await repository.UpdateAsync(created);

        var updated = await repository.GetByNameAsync("UpdateTest");

        // Assert
        updated.Should().NotBeNull();
        updated!.DestinationUrl.Should().Be("https://newurl.com/xml");
        updated.Version.Should().BeGreaterThan(originalVersion);
    }

    [Test]
    public async Task DeleteAsync_Should_RemoveIntegration_AndCascadeDelete()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "DeleteTest",
            Endpoint = "/api/delete",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/json",
            FieldMappings =
            [
                new FieldMappingEntity
                {
                    Source = "$.test",
                    Destination = "$.result",
                    Order = 0
                }
            ]
        };

        var created = await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(created.Id);

        var result = await repository.GetByIdAsync(created.Id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAllActiveAsync_Should_ReturnOnlyActiveIntegrations()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var activeEntity = new IntegrationMappingEntity
        {
            Name = "ActiveTest",
            Endpoint = "/api/active",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/active",
            IsActive = true
        };

        var inactiveEntity = new IntegrationMappingEntity
        {
            Name = "InactiveTest",
            Endpoint = "/api/inactive",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/inactive",
            IsActive = false
        };

        await repository.AddAsync(activeEntity);
        await repository.AddAsync(inactiveEntity);

        // Act
        var results = await repository.GetAllActiveAsync();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(x => x.IsActive.Should().BeTrue());
        results.Should().Contain(x => x.Name == "ActiveTest");
        results.Should().NotContain(x => x.Name == "InactiveTest");
    }

    [Test]
    public async Task GetGlobalStaticValuesAsync_Should_ReturnOnlyGlobalValues()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "StaticValuesTest",
            Endpoint = "/api/staticvalues",
            SourceType = "JSON",
            DestinationType = "JSON",
            DestinationUrl = "https://example.com/test",
            StaticValues =
            [
                new StaticValueEntity
                {
                    Key = "GlobalKey",
                    Value = "GlobalValue",
                    IsGlobal = true
                },
                new StaticValueEntity
                {
                    Key = "LocalKey",
                    Value = "LocalValue",
                    IsGlobal = false
                }
            ]
        };

        await repository.AddAsync(entity);

        // Act
        var results = await repository.GetGlobalStaticValuesAsync();

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(x => x.IsGlobal.Should().BeTrue());
        results.Should().Contain(x => x.Key == "GlobalKey");
        results.Should().NotContain(x => x.Key == "LocalKey");
    }

    [Test]
    public async Task SoapConfiguration_Should_BePersisted_WithHeadersAndBodyFields()
    {
        // Arrange
        using var scope = _serviceProvider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationMappingRepository>();

        var entity = new IntegrationMappingEntity
        {
            Name = "SoapTest",
            Endpoint = "/api/soap",
            SourceType = "JSON",
            DestinationType = "SOAP",
            DestinationUrl = "https://example.com/soap",
            SoapConfig = new SoapConfigEntity
            {
                BodyWrapperFieldXPath = "//soap:Body/Request",
                Fields =
                [
                    new SoapFieldEntity
                    {
                        FieldType = "Header",
                        XPath = "//soap:Header/ApiKey",
                        Source = "$.apiKey",
                        Order = 0
                    },
                    new SoapFieldEntity
                    {
                        FieldType = "Body",
                        XPath = "//Request/Customer",
                        Source = "$.customer.name",
                        Order = 0
                    }
                ]
            }
        };

        // Act
        var result = await repository.AddAsync(entity);
        var retrieved = await repository.GetByNameAsync("SoapTest");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.SoapConfig.Should().NotBeNull();
        retrieved.SoapConfig!.BodyWrapperFieldXPath.Should().Be("//soap:Body/Request");
        retrieved.SoapConfig.Fields.Should().HaveCount(2);
        retrieved.SoapConfig.Fields.Should().Contain(x => x.FieldType == "Header");
        retrieved.SoapConfig.Fields.Should().Contain(x => x.FieldType == "Body");
    }
}
