using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using QuickApiMapper.Behaviors;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public sealed class BehaviorPipelineTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<ILogger<AuthenticationBehavior>> _authLogger;
    private readonly Mock<ILogger<HttpClientConfigurationBehavior>> _httpConfigLogger;
    private readonly Mock<ILogger<TimingBehavior>> _timingLogger;
    private readonly Mock<ILogger<ValidationBehavior>> _validationLogger;
    private readonly TestHttpClientFactory _httpClientFactory;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly HttpClient _httpClient;

    public BehaviorPipelineTests()
    {
        _authLogger = new Mock<ILogger<AuthenticationBehavior>>();
        _httpConfigLogger = new Mock<ILogger<HttpClientConfigurationBehavior>>();
        _timingLogger = new Mock<ILogger<TimingBehavior>>();
        _validationLogger = new Mock<ILogger<ValidationBehavior>>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClientFactory = new TestHttpClientFactory(_httpMessageHandler.Object);

        _httpClient = new HttpClient(_httpMessageHandler.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpClientFactory>(_httpClientFactory);
        services.AddSingleton(_httpClient);
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [TearDown]
    public void TearDown()
    {
        _authLogger.Reset();
        _httpConfigLogger.Reset();
        _timingLogger.Reset();
        _validationLogger.Reset();
    }
    
    private MappingContext CreateMappingContext(IEnumerable<FieldMapping>? mappings = null)
    {
        return new MappingContext
        {
            Mappings = mappings ?? [new FieldMapping("test.source", "test.destination")],
            Source = new { test = "data" },
            ServiceProvider = _serviceProvider,
            CancellationToken = CancellationToken.None
        };
    }

    #region ValidationBehavior Tests

    [Test]
    public async Task ValidationBehavior_WithValidContext_ShouldPass()
    {
        // Arrange
        var behavior = new ValidationBehavior(_validationLogger.Object);
        var context = CreateMappingContext();

        // Act & Assert
        await behavior.ExecuteAsync(context);

        // Verify logging
        _validationLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting validation behavior")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Test]
    public void ValidationBehavior_WithNoMappings_ShouldThrow()
    {
        // Arrange
        var behavior = new ValidationBehavior(_validationLogger.Object);
        var context = CreateMappingContext(Array.Empty<FieldMapping>());

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => behavior.ExecuteAsync(context));

        Assert.That(ex.Message, Contains.Substring("No mappings provided"));
    }

    [Test]
    public void ValidationBehavior_WithNoDataSources_ShouldThrow()
    {
        // Arrange
        var behavior = new ValidationBehavior(_validationLogger.Object);
        var context = new MappingContext
        {
            Mappings = [new FieldMapping("test.source", "test.destination")],
            Source = null,
            Destination = null,
            ServiceProvider = _serviceProvider,
            CancellationToken = CancellationToken.None
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => behavior.ExecuteAsync(context));

        Assert.That(ex.Message, Contains.Substring("No data sources provided"));
    }

    [Test]
    public void ValidationBehavior_WithEmptySourcePaths_ShouldThrow()
    {
        // Arrange
        var behavior = new ValidationBehavior(_validationLogger.Object);
        var context = CreateMappingContext(new[]
        {
            new FieldMapping("", "test.destination"),
            new FieldMapping("valid.source", "test.destination")
        });

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => behavior.ExecuteAsync(context));

        Assert.That(ex.Message, Contains.Substring("mappings with empty source paths"));
    }

    [Test]
    public void ValidationBehavior_OrderProperty_ShouldBe50()
    {
        // Arrange
        var behavior = new ValidationBehavior(_validationLogger.Object);

        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(behavior.Order, Is.EqualTo(50));
            Assert.That(behavior.Name, Is.EqualTo("Validation"));
        });
    }

    #endregion

    #region TimingBehavior Tests

    [Test]
    public async Task TimingBehavior_WithSuccessfulExecution_ShouldMeasureTime()
    {
        // Arrange
        var behavior = new TimingBehavior(_timingLogger.Object);
        var context = CreateMappingContext();
        var expectedResult = MappingResult.Success();

        // Act
        var result = await behavior.ExecuteAsync(context, _ =>
        {
            Thread.Sleep(100); // Simulate work
            return Task.FromResult(expectedResult);
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Properties.ContainsKey("ExecutionTime"), Is.True);
        });

        var executionTime = (TimeSpan)result.Properties["ExecutionTime"];
        Assert.That(executionTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(50));

        // Verify logging
        _timingLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping execution completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Test]
    public async Task TimingBehavior_WithFailedExecution_ShouldReturnFailureWithTime()
    {
        // Arrange
        var behavior = new TimingBehavior(_timingLogger.Object);
        var context = CreateMappingContext();
        var expectedException = new InvalidOperationException("Test failure");

        // Act
        var result = await behavior.ExecuteAsync(context, _ =>
        {
            Thread.Sleep(50);
            throw expectedException;
        });

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Properties.ContainsKey("ExecutionTime"), Is.True);
        });

        var executionTime = (TimeSpan)result.Properties["ExecutionTime"];
        Assert.That(executionTime.TotalMilliseconds, Is.GreaterThanOrEqualTo(25));

        // Verify error logging
        _timingLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping execution failed")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Test]
    public void TimingBehavior_OrderProperty_ShouldBe10()
    {
        // Arrange
        var behavior = new TimingBehavior(_timingLogger.Object);

        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(behavior.Order, Is.EqualTo(10));
            Assert.That(behavior.Name, Is.EqualTo("Timing"));
        });
    }

    #endregion

    #region HttpClientConfigurationBehavior Tests

    [Test]
    public async Task HttpClientConfigurationBehavior_WithValidConfig_ShouldConfigureHttpClient()
    {
        // Arrange
        var config = new HttpClientConfiguration
        {
            DefaultHeaders = new Dictionary<string, string>
            {
                { "X-Custom-Header", "TestValue" },
                { "X-Another-Header", "AnotherValue" }
            },
            Timeout = TimeSpan.FromSeconds(30),
            UserAgent = "TestUserAgent/1.0"
        };

        var behavior = new HttpClientConfigurationBehavior(config, _httpConfigLogger.Object);
        var context = CreateMappingContext();

        // Act
        await behavior.ExecuteAsync(context);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(_httpClient.Timeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(_httpClient.DefaultRequestHeaders.UserAgent.ToString(), Contains.Substring("TestUserAgent/1.0"));

            // Check custom headers
            Assert.That(_httpClient.DefaultRequestHeaders.GetValues("X-Custom-Header").First(), Is.EqualTo("TestValue"));
            Assert.That(_httpClient.DefaultRequestHeaders.GetValues("X-Another-Header").First(), Is.EqualTo("AnotherValue"));

            // Check context properties
            Assert.That(context.Properties.ContainsKey("HttpClientFactory"), Is.True);
            Assert.That(context.Properties.ContainsKey("HttpClientConfig"), Is.True);
        });
    }

    [Test]
    public async Task HttpClientConfigurationBehavior_WithMinimalConfig_ShouldNotThrow()
    {
        // Arrange
        var config = new HttpClientConfiguration();
        var behavior = new HttpClientConfigurationBehavior(config, _httpConfigLogger.Object);
        var context = CreateMappingContext();

        // Act & Assert
        await behavior.ExecuteAsync(context);

        // Verify completion logging
        _httpConfigLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HTTP client configuration behavior completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
            Times.Once);
    }

    [Test]
    public void HttpClientConfigurationBehavior_OrderProperty_ShouldBe200()
    {
        // Arrange
        var config = new HttpClientConfiguration();
        var behavior = new HttpClientConfigurationBehavior(config, _httpConfigLogger.Object);

        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(behavior.Order, Is.EqualTo(200));
            Assert.That(behavior.Name, Is.EqualTo("HttpClientConfiguration"));
        });
    }

    #endregion

    #region AuthenticationBehavior Tests

    [Test]
    public async Task AuthenticationBehavior_WithValidTokenResponse_ShouldAcquireAndCacheToken()
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client",
            ClientSecret = "test-secret",
            Scope = "read write"
        };

        var tokenResponse = new
        {
            access_token = "test-access-token",
            token_type = "Bearer",
            expires_in = 3600
        };

        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var behavior = new AuthenticationBehavior(config, _httpClientFactory, _authLogger.Object);
        var context = CreateMappingContext();

        // Act
        await behavior.ExecuteAsync(context);

        // Assert
        Assert.That(context.Properties.ContainsKey("AuthToken"), Is.True);
        var token = (TokenInfo)context.Properties["AuthToken"];
        Assert.Multiple(() =>
        {
            Assert.That(token.AccessToken, Is.EqualTo("test-access-token"));
            Assert.That(token.TokenType, Is.EqualTo("Bearer"));
            Assert.That(token.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
        });
    }

    [Test]
    public async Task AuthenticationBehavior_WithCachedValidToken_ShouldNotMakeNewRequest()
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        var tokenResponse = new
        {
            access_token = "test-access-token",
            token_type = "Bearer",
            expires_in = 3600
        };

        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var behavior = new AuthenticationBehavior(config, _httpClientFactory, _authLogger.Object);
        var context1 = CreateMappingContext();
        var context2 = CreateMappingContext();

        // Act
        await behavior.ExecuteAsync(context1);
        await behavior.ExecuteAsync(context2);

        // Assert
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        // Verify both contexts have the same token
        Assert.That(context1.Properties["AuthToken"], Is.EqualTo(context2.Properties["AuthToken"]));
    }

    [Test]
    public void AuthenticationBehavior_WithFailedTokenRequest_ShouldThrow()
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        using var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        httpResponse.Content = new StringContent("Invalid credentials", Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var behavior = new AuthenticationBehavior(config, _httpClientFactory, _authLogger.Object);
        var context = CreateMappingContext();

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => behavior.ExecuteAsync(context));

        Assert.That(ex.Message, Contains.Substring("Token acquisition failed"));
    }

    [Test]
    public void AuthenticationBehavior_OrderProperty_ShouldBe100()
    {
        // Arrange
        var config = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };
        var behavior = new AuthenticationBehavior(config, _httpClientFactory, _authLogger.Object);

        Assert.Multiple(() =>
        {
            // Act & Assert
            Assert.That(behavior.Order, Is.EqualTo(100));
            Assert.That(behavior.Name, Is.EqualTo("Authentication"));
        });
    }

    #endregion


#pragma warning disable IDISP013
#pragma warning disable IDISP014
    private sealed class TestHttpClientFactory(
        HttpMessageHandler handler
    ) : IHttpClientFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

        public HttpClient CreateClient(string? name)
        {
            return string.IsNullOrEmpty(name)
                ? _clients.GetOrAdd(Guid.NewGuid().ToString(), new HttpClient(handler))
                : _clients.GetOrAdd(name, _ => new HttpClient(handler));
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
                client.Dispose();
        }
    }
#pragma warning restore IDISP014
#pragma warning restore IDISP014

    #region Integration Tests

    [Test]
    public async Task BehaviorPipeline_ExecutionOrder_ShouldBeRespected()
    {
        // Arrange
        var executionOrder = new List<string>();

        var validationBehavior = new ValidationBehavior(_validationLogger.Object);
        var authBehavior = new AuthenticationBehavior(
            new AuthenticationConfig
            {
                TokenEndpoint = "https://auth.example.com/token",
                ClientId = "test-client",
                ClientSecret = "test-secret"
            },
            _httpClientFactory,
            _authLogger.Object);

        var httpConfigBehavior = new HttpClientConfigurationBehavior(
            new HttpClientConfiguration(),
            _httpConfigLogger.Object);

        var timingBehavior = new TimingBehavior(_timingLogger.Object);

        // Setup token response
        var tokenResponse = new { access_token = "token", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var context = CreateMappingContext();

        // Act - Execute behaviors in the order they would be executed in the pipeline
        var preRunBehaviors = new IBehavior[] { validationBehavior, authBehavior, httpConfigBehavior }
            .OrderBy(b => b.Order)
            .Cast<IPreRunBehavior>()
            .ToList();

        foreach (var behavior in preRunBehaviors)
        {
            await behavior.ExecuteAsync(context);
            executionOrder.Add(behavior.Name);
        }

        // Execute timing behavior around the "core" logic
        var result = await timingBehavior.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("CoreLogic");
            return Task.FromResult(MappingResult.Success());
        });

        // Assert
        var expected = new[] { "Validation", "Authentication", "HttpClientConfiguration", "CoreLogic" };
        Assert.Multiple(() =>
        {
            Assert.That(executionOrder, Is.EqualTo(expected));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Properties.ContainsKey("ExecutionTime"), Is.True);
        });
    }

    [Test]
    public async Task BehaviorPipeline_WithAuthenticationAndHttpConfig_ShouldConfigureHttpClientWithAuth()
    {
        // Arrange
        var authConfig = new AuthenticationConfig
        {
            TokenEndpoint = "https://auth.example.com/token",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        var httpConfig = new HttpClientConfiguration
        {
            DefaultHeaders = new Dictionary<string, string> { { "X-Custom", "Value" } },
            Timeout = TimeSpan.FromSeconds(60)
        };

        var tokenResponse = new { access_token = "test-token", expires_in = 3600 };
        var responseContent = JsonSerializer.Serialize(tokenResponse);
        using var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var authBehavior = new AuthenticationBehavior(authConfig, _httpClientFactory, _authLogger.Object);
        var httpConfigBehavior = new HttpClientConfigurationBehavior(httpConfig, _httpConfigLogger.Object);
        var context = CreateMappingContext();

        // Act
        await authBehavior.ExecuteAsync(context);
        await httpConfigBehavior.ExecuteAsync(context);

        // Assert
        Assert.That(_httpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("Bearer"));
            Assert.That(_httpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo("test-token"));
            Assert.That(_httpClient.DefaultRequestHeaders.GetValues("X-Custom").First(), Is.EqualTo("Value"));
            Assert.That(_httpClient.Timeout, Is.EqualTo(TimeSpan.FromSeconds(60)));
        });
    }

    #endregion

    public void Dispose()
    {
        _httpClient.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        _httpMessageHandler?.Object.Dispose();
        _httpClientFactory.Dispose();
    }
}