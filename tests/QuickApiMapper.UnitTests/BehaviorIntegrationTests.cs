using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using QuickApiMapper.Behaviors;
using QuickApiMapper.Contracts;
using QuickApiMapper.UnitTests.Infrastructure;

namespace QuickApiMapper.UnitTests;

[TestFixture]
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public sealed class BehaviorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private BehaviorTestCollection _behaviorCollection;
    private readonly HttpClient _httpClient;
    private readonly TestHttpClientFactory _httpClientFactory;

    public BehaviorIntegrationTests()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandler.Object);

        // Create a test implementation instead of mocking
        _httpClientFactory = new TestHttpClientFactory(_httpMessageHandler.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(_httpClientFactory);
        // Register the same HttpClient instance that the test uses
        services.AddSingleton<HttpClient>(_httpClient);
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        _serviceProvider = services.BuildServiceProvider();
        _behaviorCollection = BehaviorTestCollectionFactory.Create();
    }

    [SetUp]
    public void SetUp()
    {
        // Create a fresh behavior collection for each test to prevent behavior accumulation
        _behaviorCollection = BehaviorTestCollectionFactory.Create();
    }

    [TearDown]
    public void TearDown()
    {
        _httpMessageHandler.Reset();
    }

    private MappingContext CreateMappingContext(IEnumerable<FieldMapping>? mappings = null)
        => new()
        {
            Mappings = mappings ?? [new FieldMapping("user.name", "UserName")],
            Source = new { user = new { name = "John Doe", email = "john@example.com" } },
            ServiceProvider = _serviceProvider,
            CancellationToken = CancellationToken.None
        };

    [Test]
    public async Task FullPipeline_WithAllBehaviors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var tokenResponse = new { access_token = "integration-token", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var authConfig = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "integration-client",
            ClientSecret = "integration-secret",
            Scope = "api:read api:write"
        };

        var httpConfig = new HttpClientConfiguration
        {
            DefaultHeaders = new Dictionary<string, string>
            {
                { "X-API-Version", "2.0" },
                { "X-Client-ID", "integration-test" }
            },
            Timeout = TimeSpan.FromSeconds(45),
            UserAgent = "QuickApiMapper-IntegrationTest/1.0"
        };

        var validationBehavior = new ValidationBehavior(_serviceProvider.GetRequiredService<ILogger<ValidationBehavior>>());
        var authBehavior = new AuthenticationBehavior(authConfig, _httpClientFactory,
            _serviceProvider.GetRequiredService<ILogger<AuthenticationBehavior>>());
        var httpConfigBehavior =
            new HttpClientConfigurationBehavior(httpConfig, _serviceProvider.GetRequiredService<ILogger<HttpClientConfigurationBehavior>>());
        var timingBehavior = new TimingBehavior(_serviceProvider.GetRequiredService<ILogger<TimingBehavior>>());

        // Build pipeline with behaviors
        var pipeline = _behaviorCollection
            .AddPreRunBehavior(validationBehavior)
            .AddPreRunBehavior(authBehavior)
            .AddPreRunBehavior(httpConfigBehavior)
            .AddWholeRunBehavior(timingBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext();

        // Act
        var result = await pipeline.ExecuteAsync(context, async _ =>
        {
            // Simulate core mapping logic
            await Task.Delay(100);
            return MappingResult.Success();
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Properties.ContainsKey("ExecutionTime"), Is.True);

            // Verify authentication was configured
            Assert.That(context.Properties.ContainsKey("AuthToken"), Is.True);
        });
        var token = (TokenInfo)context.Properties["AuthToken"];
        Assert.Multiple(() =>
        {
            Assert.That(token.AccessToken, Is.EqualTo("integration-token"));

            // Verify HTTP client configuration
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo("integration-token"));
            Assert.That(_httpClient.DefaultRequestHeaders.GetValues("X-API-Version").First(), Is.EqualTo("2.0"));
            Assert.That(_httpClient.Timeout, Is.EqualTo(TimeSpan.FromSeconds(45)));
        });

        // Verify timing was captured
        var executionTime = (TimeSpan)result.Properties["ExecutionTime"];
        Assert.That(executionTime.TotalMilliseconds, Is.GreaterThan(50));
    }

    [Test]
    public void Pipeline_WithAuthenticationFailure_ShouldPropagateError()
    {
        // Arrange
        using var failureResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        failureResponse.Content = new StringContent("Invalid client credentials", Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(failureResponse);

        var authConfig = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "invalid-client",
            ClientSecret = "invalid-secret"
        };
        var timingBehavior = new TimingBehavior(_serviceProvider.GetRequiredService<ILogger<TimingBehavior>>());
        var context = CreateMappingContext();
        var authBehavior = new AuthenticationBehavior(
            authConfig,
            _httpClientFactory,
            _serviceProvider.GetRequiredService<ILogger<AuthenticationBehavior>>());
        var pipeline = _behaviorCollection
            .AddPreRunBehavior(authBehavior)
            .AddWholeRunBehavior(timingBehavior)
            .BuildPipeline(_serviceProvider);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context, _ => Task.FromResult(MappingResult.Success())));
        Assert.That(ex.Message, Contains.Substring("Token acquisition failed"));
    }

    [Test]
    public void Pipeline_WithValidationFailure_ShouldFailEarly()
    {
        // Arrange
        var validationBehavior = new ValidationBehavior(_serviceProvider.GetRequiredService<ILogger<ValidationBehavior>>());
        var timingBehavior = new TimingBehavior(_serviceProvider.GetRequiredService<ILogger<TimingBehavior>>());

        var pipeline = _behaviorCollection
            .AddPreRunBehavior(validationBehavior)
            .AddWholeRunBehavior(timingBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext([]);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(context, _ => Task.FromResult(MappingResult.Success())));

        Assert.That(ex.Message, Contains.Substring("No mappings provided"));
    }

    [Test]
    public async Task Pipeline_WithConcurrentExecution_ShouldHandleTokenCaching()
    {
        // Arrange
        var tokenResponse = new { access_token = "concurrent-token", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var authConfig = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "concurrent-client",
            ClientSecret = "concurrent-secret"
        };
        var contexts = Enumerable.Range(0, 5).Select(_ => CreateMappingContext()).ToList();
        var authBehavior = new AuthenticationBehavior(authConfig, _httpClientFactory,
            _serviceProvider.GetRequiredService<ILogger<AuthenticationBehavior>>());
        var tasks = contexts.Select(async context =>
        {
            await authBehavior.ExecuteAsync(context);
            return (TokenInfo)context.Properties["AuthToken"];
        }).ToList();

        var tokens = await Task.WhenAll(tasks);

        // Assert
        Assert.That(tokens.Length, Is.EqualTo(5));
        Assert.That(tokens.All(t => t.AccessToken == "concurrent-token"), Is.True);

        // Verify only one token request was made despite concurrent execution
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task Pipeline_WithExpiredTokenCache_ShouldRefreshToken()
    {
        // Arrange
        var firstTokenResponse = new { access_token = "expired-token", expires_in = 1 }; // 1 second expiry
        var secondTokenResponse = new { access_token = "refreshed-token", expires_in = 3600 };

        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(firstTokenResponse), Encoding.UTF8, "application/json")
        });
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(secondTokenResponse), Encoding.UTF8, "application/json")
        });

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responses.Dequeue());

        var authConfig = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "refresh-client",
            ClientSecret = "refresh-secret",
            TokenCacheExpiry = TimeSpan.FromSeconds(1)
        };
        var context1 = CreateMappingContext();
        var context2 = CreateMappingContext();
        var authBehavior = new AuthenticationBehavior(
            authConfig,
            _httpClientFactory,
            _serviceProvider.GetRequiredService<ILogger<AuthenticationBehavior>>());
        await authBehavior.ExecuteAsync(context1);

        // Wait for token to expire
        await Task.Delay(1200);

        await authBehavior.ExecuteAsync(context2);

        // Assert
        var firstToken = (TokenInfo)context1.Properties["AuthToken"];
        var secondToken = (TokenInfo)context2.Properties["AuthToken"];

        Assert.Multiple(() =>
        {
            Assert.That(firstToken.AccessToken, Is.EqualTo("expired-token"));
            Assert.That(secondToken.AccessToken, Is.EqualTo("refreshed-token"));
        });

        // Verify two token requests were made
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task Pipeline_WithPostRunBehavior_ShouldExecuteAfterCore()
    {
        // Arrange
        var executionOrder = new List<string>();
        var postRunBehavior = new TestPostRunBehavior(executionOrder);
        var timingBehavior = new TimingBehavior(_serviceProvider.GetRequiredService<ILogger<TimingBehavior>>());

        var pipeline = _behaviorCollection
            .AddPostRunBehavior(postRunBehavior)
            .AddWholeRunBehavior(timingBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext();

        // Act
        var result = await pipeline.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("CoreLogic");
            return Task.FromResult(MappingResult.Success());
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(executionOrder, Is.EqualTo(new[] { "CoreLogic", "PostRun" }));
        });
    }

    [Test]
    public async Task Pipeline_WithMultipleWholeRunBehaviors_ShouldNestCorrectly()
    {
        // Arrange
        var executionOrder = new List<string>();
        var outerBehavior = new TestWholeRunBehavior("Outer", executionOrder);
        var innerBehavior = new TestWholeRunBehavior("Inner", executionOrder);

        var pipeline = _behaviorCollection
            .AddWholeRunBehavior(outerBehavior)
            .AddWholeRunBehavior(innerBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext();

        // Act
        var result = await pipeline.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("Core");
            return Task.FromResult(MappingResult.Success());
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(executionOrder, Is.EqualTo(new[] { "Outer-Start", "Inner-Start", "Core", "Inner-End", "Outer-End" }));
        });
    }

    [Test]
    public async Task BehaviorTestCollection_WithFluentAPI_ShouldBuildCorrectPipeline()
    {
        // Arrange
        var executionOrder = new List<string>();
        var preRunBehavior = new TestPreRunBehavior(executionOrder);
        var postRunBehavior = new TestPostRunBehavior(executionOrder);
        var wholeRunBehavior = new TestWholeRunBehavior("Test", executionOrder);

        // Test fluent API
        var pipeline = BehaviorTestCollectionFactory.Create()
            .AddPreRunBehavior(preRunBehavior)
            .AddPostRunBehavior(postRunBehavior)
            .AddWholeRunBehavior(wholeRunBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext();

        // Act
        var result = await pipeline.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("Core");
            return Task.FromResult(MappingResult.Success());
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(executionOrder, Is.EqualTo(new[] { "PreRun", "Test-Start", "Core", "Test-End", "PostRun" }));
        });
    }

    [Test]
    public async Task BehaviorTestCollection_WithFactoryMethods_ShouldBuildCorrectPipeline()
    {
        // Arrange
        var executionOrder = new List<string>();
        var preRunBehavior = new TestPreRunBehavior(executionOrder);
        var postRunBehavior = new TestPostRunBehavior(executionOrder);

        // Test factory methods
        var pipeline = BehaviorTestCollectionFactory
            .WithPreRunBehavior(preRunBehavior)
            .AddPostRunBehavior(postRunBehavior)
            .BuildPipeline(_serviceProvider);

        var context = CreateMappingContext();

        // Act
        var result = await pipeline.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("Core");
            return Task.FromResult(MappingResult.Success());
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(executionOrder, Is.EqualTo(new[] { "PreRun", "Core", "PostRun" }));
        });
    }

    private class TestPreRunBehavior(
        List<string> executionOrder
    ) :
        IPreRunBehavior
    {
        public string Name => "TestPreRun";
        public int Order => 100;

        public Task ExecuteAsync(MappingContext context)
        {
            executionOrder.Add("PreRun");
            return Task.CompletedTask;
        }
    }

    private class TestPostRunBehavior : IPostRunBehavior
    {
        private readonly List<string> _executionOrder;

        public TestPostRunBehavior(List<string> executionOrder)
        {
            _executionOrder = executionOrder;
        }

        public string Name => "TestPostRun";
        public int Order => 100;

        public Task ExecuteAsync(MappingContext context, MappingResult result)
        {
            _executionOrder.Add("PostRun");
            return Task.CompletedTask;
        }
    }

    private class TestWholeRunBehavior(
        string name,
        List<string> executionOrder
    ) :
        IWholeRunBehavior
    {
        public string Name => $"Test{name}";
        public int Order => name == "Outer" ? 10 : 20;

        public async Task<MappingResult> ExecuteAsync(MappingContext context, Func<MappingContext, Task<MappingResult>> next)
        {
            executionOrder.Add($"{name}-Start");
            var result = await next(context);
            executionOrder.Add($"{name}-End");
            return result;
        }
    }


#pragma warning disable IDISP013
#pragma warning disable IDISP014
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created")]
    private sealed class TestHttpClientFactory(
        HttpMessageHandler handler
    ) : IHttpClientFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new();
        private readonly Guid _id = Guid.NewGuid();

        public HttpClient CreateClient(string? name)
        {
            var client = _clients.GetOrAdd(name ?? _id.ToString(), 
                _ => new HttpClient(handler));
            
            // confirm client is not disposed
            try
            {
                // This will throw if the client is disposed
                client.Timeout = client.Timeout;
                return client;
            } catch (ObjectDisposedException)
            {
                // Create a new client if the existing one is disposed
                var newClient = new HttpClient(handler)
                {
                    // transfer settings from the old client
                    Timeout = client.Timeout,
                    BaseAddress = client.BaseAddress,
                    DefaultRequestVersion = client.DefaultRequestVersion,
                    DefaultVersionPolicy = client.DefaultVersionPolicy,
                    MaxResponseContentBufferSize = client.MaxResponseContentBufferSize
                    
                };

                foreach (var header in client.DefaultRequestHeaders)
                {
                    newClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
                _clients[name ?? _id.ToString()] = newClient;
                
                return newClient;
            }
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
                client.Dispose();
        }
    }
#pragma warning restore IDISP014
#pragma warning restore IDISP013

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _httpClient.Dispose();
        _httpClientFactory.Dispose();
    }
}