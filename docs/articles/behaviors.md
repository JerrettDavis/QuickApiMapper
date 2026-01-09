# Behaviors

Behaviors provide a powerful way to add cross-cutting concerns to your integrations. This guide covers built-in behaviors, creating custom behaviors, and advanced scenarios.

## Overview

Behaviors wrap the mapping pipeline and can execute logic before, after, or around the mapping process. Common use cases include:

- Authentication
- Input/output validation
- Logging and timing
- Message capture
- Error handling
- Caching
- Rate limiting

## Behavior Types

QuickApiMapper supports three types of behaviors:

### 1. IWholeRunBehavior

Wraps the entire mapping execution, including pre-map and post-map phases.

**Use Cases**:
- Timing measurements
- Message capture
- Transaction management
- Global error handling

**Execution Order**: 100-999

### 2. IPreMapBehavior

Executes before the mapping process.

**Use Cases**:
- Authentication
- Input validation
- Request preprocessing
- Context enrichment

**Execution Order**: 1000-1999

### 3. IPostMapBehavior

Executes after the mapping process.

**Use Cases**:
- Output validation
- Response postprocessing
- Logging
- Notifications

**Execution Order**: 2000-2999

## Built-in Behaviors

### TimingBehavior

Measures execution time and logs performance metrics.

**Type**: `IWholeRunBehavior`
**Order**: 100
**Location**: `QuickApiMapper.Behaviors/TimingBehavior.cs`

**Implementation**:
```csharp
public class TimingBehavior : IWholeRunBehavior
{
 public int Order => 100;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 var stopwatch = Stopwatch.StartNew();

 var result = await next(context);

 stopwatch.Stop();
 context.Metadata["ExecutionTimeMs"] = stopwatch.ElapsedMilliseconds;

 _logger.LogInformation(
 "Integration {IntegrationId} completed in {Duration}ms",
 context.IntegrationId,
 stopwatch.ElapsedMilliseconds);

 return result;
 }
}
```

**Configuration**:
Automatically registered. No configuration required.

### AuthenticationBehavior

Adds authentication headers to outbound requests.

**Type**: `IPreMapBehavior`
**Order**: 1000
**Location**: `QuickApiMapper.Behaviors/AuthenticationBehavior.cs`

**Supported Auth Types**:
- Bearer Token
- API Key
- Basic Authentication
- Custom Headers

**Implementation**:
```csharp
public class AuthenticationBehavior : IPreMapBehavior
{
 public int Order => 1000;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 var authConfig = context.Configuration.AuthenticationConfig;

 if (authConfig != null)
 {
 switch (authConfig.Type)
 {
 case "Bearer":
 context.Headers["Authorization"] = $"Bearer {authConfig.Token}";
 break;

 case "ApiKey":
 context.Headers[authConfig.HeaderName] = authConfig.ApiKey;
 break;

 case "Basic":
 var credentials = Convert.ToBase64String(
 Encoding.UTF8.GetBytes($"{authConfig.Username}:{authConfig.Password}"));
 context.Headers["Authorization"] = $"Basic {credentials}";
 break;
 }
 }

 return await next(context);
 }
}
```

**Configuration** (per integration):
```json
{
 "authenticationConfig": {
 "type": "Bearer",
 "token": "your-token-here"
 }
}
```

### ValidationBehavior

Validates input and output against JSON schemas.

**Type**: `IPreMapBehavior` and `IPostMapBehavior`
**Order**: 1100 (pre), 2100 (post)
**Location**: `QuickApiMapper.Behaviors/ValidationBehavior.cs`

**Features**:
- JSON Schema validation
- Input validation before mapping
- Output validation after mapping
- Configurable fail-on-error behavior

**Implementation**:
```csharp
public class ValidationBehavior : IPreMapBehavior, IPostMapBehavior
{
 public int Order => 1100; // For pre-map

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 // Validate input
 if (!string.IsNullOrEmpty(context.Configuration.InputSchema))
 {
 var isValid = await ValidateAsync(
 context.SourceData,
 context.Configuration.InputSchema);

 if (!isValid && context.Configuration.FailOnValidationError)
 {
 return new MappingResult
 {
 IsSuccess = false,
 ErrorMessage = "Input validation failed"
 };
 }
 }

 return await next(context);
 }
}
```

**Configuration**:
```json
{
 "inputSchema": "{\"type\": \"object\", \"required\": [\"customer\"]}",
 "outputSchema": "{\"type\": \"object\", \"required\": [\"Customer\"]}",
 "failOnValidationError": true
}
```

### HttpClientConfigurationBehavior

Configures HTTP client settings for outbound requests.

**Type**: `IPreMapBehavior`
**Order**: 1050
**Location**: `QuickApiMapper.Behaviors/HttpClientConfigurationBehavior.cs`

**Features**:
- Timeout configuration
- Custom headers
- SSL certificate validation
- Proxy configuration

**Implementation**:
```csharp
public class HttpClientConfigurationBehavior : IPreMapBehavior
{
 public int Order => 1050;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 if (context.Configuration.HttpConfig != null)
 {
 context.HttpClient.Timeout = TimeSpan.FromSeconds(
 context.Configuration.HttpConfig.TimeoutSeconds);

 foreach (var header in context.Configuration.HttpConfig.Headers)
 {
 context.HttpClient.DefaultRequestHeaders.Add(
 header.Key,
 header.Value);
 }
 }

 return await next(context);
 }
}
```

### MessageCaptureBehavior

Captures input and output messages for debugging and auditing.

**Type**: `IWholeRunBehavior`
**Order**: 200
**Location**: `QuickApiMapper.Behaviors/MessageCaptureBehavior.cs`

**Features**:
- Captures request and response payloads
- Correlation IDs for request tracking
- Timestamps and duration tracking
- Error message capture
- Configurable payload size limits

See [Message Capture](message-capture.md) for detailed documentation.

## Creating Custom Behaviors

### Step 1: Implement the Behavior Interface

Choose the appropriate interface based on when the behavior should execute.

**Example - Pre-Map Behavior**:
```csharp
using QuickApiMapper.Contracts;

namespace MyCompany.CustomBehaviors;

public class ApiKeyRotationBehavior : IPreMapBehavior
{
 private readonly IApiKeyService _apiKeyService;
 private readonly ILogger<ApiKeyRotationBehavior> _logger;

 public ApiKeyRotationBehavior(
 IApiKeyService apiKeyService,
 ILogger<ApiKeyRotationBehavior> logger)
 {
 _apiKeyService = apiKeyService;
 _logger = logger;
 }

 // Lower order = earlier execution
 public int Order => 950;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 // Get fresh API key
 var apiKey = await _apiKeyService.GetCurrentKeyAsync();

 // Add to context headers
 context.Headers["X-API-Key"] = apiKey;

 _logger.LogDebug("API key added to request headers");

 // Continue pipeline
 return await next(context);
 }
}
```

### Step 2: Register the Behavior

Register in the DI container:

**Program.cs**:
```csharp
using MyCompany.CustomBehaviors;

builder.Services.AddSingleton<IPreMapBehavior, ApiKeyRotationBehavior>();
```

### Step 3: Use the Behavior

The behavior automatically executes for all integrations based on its order.

## Advanced Custom Behaviors

### Whole-Run Behavior with Error Handling

```csharp
public class ErrorHandlingBehavior : IWholeRunBehavior
{
 private readonly ILogger<ErrorHandlingBehavior> _logger;
 private readonly INotificationService _notificationService;

 public int Order => 50; // Execute early

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 try
 {
 return await next(context);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex,
 "Integration {IntegrationId} failed with error",
 context.IntegrationId);

 // Send notification
 await _notificationService.SendAlertAsync(
 $"Integration {context.IntegrationId} failed",
 ex.Message);

 return new MappingResult
 {
 IsSuccess = false,
 ErrorMessage = ex.Message,
 Exception = ex
 };
 }
 }
}
```

### Conditional Behavior

Execute behavior logic only for specific integrations:

```csharp
public class ConditionalAuthBehavior : IPreMapBehavior
{
 public int Order => 1000;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 // Only add auth for external integrations
 if (context.Configuration.Tags?.Contains("external") == true)
 {
 context.Headers["Authorization"] = await GetTokenAsync();
 }

 return await next(context);
 }
}
```

### Caching Behavior

Cache integration results:

```csharp
public class CachingBehavior : IWholeRunBehavior
{
 private readonly IDistributedCache _cache;
 private readonly ILogger<CachingBehavior> _logger;

 public int Order => 150;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 // Generate cache key
 var cacheKey = $"mapping:{context.IntegrationId}:{ComputeHash(context.SourceData)}";

 // Try get from cache
 var cached = await _cache.GetStringAsync(cacheKey);
 if (cached != null)
 {
 _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
 return JsonSerializer.Deserialize<MappingResult>(cached);
 }

 // Execute mapping
 var result = await next(context);

 // Cache successful results
 if (result.IsSuccess)
 {
 await _cache.SetStringAsync(
 cacheKey,
 JsonSerializer.Serialize(result),
 new DistributedCacheEntryOptions
 {
 AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
 });
 }

 return result;
 }

 private string ComputeHash(string input)
 {
 using var sha256 = SHA256.Create();
 var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
 return Convert.ToBase64String(hash);
 }
}
```

### Rate Limiting Behavior

Implement rate limiting per integration:

```csharp
public class RateLimitingBehavior : IPreMapBehavior
{
 private readonly IRateLimiter _rateLimiter;

 public int Order => 900;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 var isAllowed = await _rateLimiter.CheckAsync(
 context.IntegrationId,
 maxRequests: 100,
 window: TimeSpan.FromMinutes(1));

 if (!isAllowed)
 {
 return new MappingResult
 {
 IsSuccess = false,
 ErrorMessage = "Rate limit exceeded",
 StatusCode = 429
 };
 }

 return await next(context);
 }
}
```

### Retry Behavior

Retry failed requests with exponential backoff:

```csharp
public class RetryBehavior : IWholeRunBehavior
{
 private readonly ILogger<RetryBehavior> _logger;

 public int Order => 75;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 var maxRetries = 3;
 var delay = TimeSpan.FromSeconds(1);

 for (int attempt = 1; attempt <= maxRetries; attempt++)
 {
 try
 {
 var result = await next(context);

 if (result.IsSuccess || !IsTransientError(result))
 {
 return result;
 }

 if (attempt < maxRetries)
 {
 _logger.LogWarning(
 "Attempt {Attempt} failed, retrying in {Delay}ms",
 attempt,
 delay.TotalMilliseconds);

 await Task.Delay(delay);
 delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
 }
 }
 catch (Exception ex) when (IsTransientException(ex))
 {
 if (attempt == maxRetries)
 throw;

 await Task.Delay(delay);
 delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
 }
 }

 return new MappingResult
 {
 IsSuccess = false,
 ErrorMessage = "Max retries exceeded"
 };
 }

 private bool IsTransientError(MappingResult result)
 {
 return result.StatusCode is 408 or 429 or 503 or 504;
 }

 private bool IsTransientException(Exception ex)
 {
 return ex is HttpRequestException or TimeoutException;
 }
}
```

## Behavior Pipeline Execution

The behavior pipeline executes behaviors in order:

```
Request
 │
 ▼
┌─────────────────────────────────────┐
│ WholeRunBehaviors (Order: 50-999) │
│ ┌───────────────────────────────┐ │
│ │ PreMapBehaviors (1000-1999) │ │
│ │ ┌─────────────────────────┐ │ │
│ │ │ Mapping Execution │ │ │
│ │ └─────────────────────────┘ │ │
│ │ PostMapBehaviors (2000-2999) │ │
│ └───────────────────────────────┘ │
└─────────────────────────────────────┘
 │
 ▼
Response
```

**Example Execution Order**:
1. ErrorHandlingBehavior (Order: 50)
2. TimingBehavior (Order: 100)
3. MessageCaptureBehavior (Order: 200)
4. ApiKeyRotationBehavior (Order: 950)
5. AuthenticationBehavior (Order: 1000)
6. HttpClientConfigurationBehavior (Order: 1050)
7. ValidationBehavior (Order: 1100)
8. **→ Mapping Execution**
9. ValidationBehavior - Post (Order: 2100)
10. MessageCaptureBehavior - Capture Output
11. TimingBehavior - Record Duration
12. ErrorHandlingBehavior - Error Handling

## Best Practices

### 1. Choose the Right Behavior Type

- Use `IWholeRunBehavior` for:
 - Timing, error handling, transactions
 - Wrapping entire execution

- Use `IPreMapBehavior` for:
 - Authentication, validation, preprocessing
 - Modifying context before mapping

- Use `IPostMapBehavior` for:
 - Output validation, logging, notifications
 - Processing results after mapping

### 2. Set Appropriate Order

Lower order = earlier execution:
- 50-99: Error handling, transactions
- 100-499: Timing, caching, message capture
- 500-999: Rate limiting, circuit breakers
- 1000-1499: Authentication, preprocessing
- 1500-1999: Input validation
- 2000-2499: Logging, notifications
- 2500-2999: Output validation

### 3. Handle Errors Gracefully

```csharp
public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
{
 try
 {
 // Behavior logic
 return await next(context);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Behavior failed");

 // Either:
 // 1. Return error result
 return new MappingResult { IsSuccess = false, ErrorMessage = ex.Message };

 // 2. Or rethrow to let higher-level behavior handle
 throw;
 }
}
```

### 4. Use Dependency Injection

Inject services instead of creating instances:

```csharp
public class MyBehavior : IPreMapBehavior
{
 private readonly IMyService _service;
 private readonly ILogger<MyBehavior> _logger;

 public MyBehavior(IMyService service, ILogger<MyBehavior> logger)
 {
 _service = service;
 _logger = logger;
 }
}
```

### 5. Keep Behaviors Focused

Each behavior should have a single responsibility:

 **Good**: `AuthenticationBehavior` only handles authentication
 **Bad**: `AuthAndValidationBehavior` does multiple things

### 6. Make Behaviors Testable

Write unit tests:

```csharp
[Test]
public async Task AuthenticationBehavior_AddsBearerToken()
{
 // Arrange
 var behavior = new AuthenticationBehavior();
 var context = new MappingContext
 {
 Configuration = new ApiMappingConfig
 {
 AuthenticationConfig = new AuthConfig
 {
 Type = "Bearer",
 Token = "test-token"
 }
 }
 };

 // Act
 await behavior.ExecuteAsync(context, ctx => Task.FromResult(new MappingResult()));

 // Assert
 Assert.AreEqual("Bearer test-token", context.Headers["Authorization"]);
}
```

## Troubleshooting

### Behavior Not Executing

**Solutions**:
- Verify behavior is registered in DI container
- Check behavior order - may be after another behavior that short-circuits
- Ensure interface is implemented correctly

### Wrong Execution Order

**Solutions**:
- Review `Order` property values
- Lower order = earlier execution
- Check for conflicting order values

### Behavior Throws Exception

**Solutions**:
- Add try-catch in behavior
- Log errors for debugging
- Consider using error handling behavior wrapper

## Next Steps

- [Architecture](architecture.md) - Understand the behavior pipeline
- [Creating Integrations](creating-integrations.md) - Use behaviors in integrations
- [Message Capture](message-capture.md) - Capture messages with behaviors
