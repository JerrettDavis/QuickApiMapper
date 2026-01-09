# Architecture Overview

This document provides a comprehensive overview of QuickApiMapper's architecture, design patterns, and core components.

## High-Level Architecture

QuickApiMapper follows a modular, layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│ Presentation Layer │
│ ┌──────────────────┐ ┌──────────────────┐ │
│ │ Web Designer │ │ Management API │ │
│ │ (Blazor Server) │ │ (REST API) │ │
│ └──────────────────┘ └──────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
 │
 ▼
┌─────────────────────────────────────────────────────────────────┐
│ Runtime Layer │
│ ┌──────────────────────────────────────────────────────────┐ │
│ │ Runtime Web API │ │
│ │ (Receives requests, executes integrations) │ │
│ └──────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
 │
 ┌─────────────────────┼─────────────────────┐
 ▼ ▼ ▼
┌──────────────┐ ┌──────────────────┐ ┌──────────────┐
│ Source │ │ Mapping Engine │ │ Destination │
│ Resolution │───▶│ + Pipeline │───▶│ Handling │
└──────────────┘ └──────────────────┘ └──────────────┘
 │
 ┌─────────────────────┼─────────────────────┐
 ▼ ▼ ▼
┌──────────────┐ ┌──────────────────┐ ┌──────────────┐
│ Transformers │ │ Behaviors │ │ Writers │
└──────────────┘ └──────────────────┘ └──────────────┘
 │
 ▼
 ┌──────────────────┐
 │ Persistence │
 │ (PostgreSQL/ │
 │ SQLite) │
 └──────────────────┘
```

## Core Components

### 1. Mapping Engine

The heart of QuickApiMapper is the `GenericMappingEngine`, which orchestrates the entire transformation process.

**Location**: `QuickApiMapper.Application/Core/GenericMappingEngine.cs`

**Responsibilities**:
- Load integration configuration from persistence
- Resolve source data using appropriate source resolver
- Execute behavior pipeline
- Apply field mappings and transformers
- Write output using destination writer
- Handle destination delivery via destination handler

**Key Interfaces**:
- `IMappingEngine` - Core mapping engine interface
- `ISourceResolver` - Resolves source data
- `IDestinationWriter` - Writes destination format
- `IDestinationHandler` - Delivers to destination system
- `ITransformer` - Transforms field values

### 2. Behavior Pipeline

The behavior pipeline provides a flexible way to add cross-cutting concerns to the mapping process.

**Location**: `QuickApiMapper.Application/Core/BehaviorPipeline.cs`

**Behavior Types**:

1. **IWholeRunBehavior** - Wraps the entire mapping execution
 - Examples: Timing, Message Capture, Error Handling
 - Order: 100-999

2. **IPreMapBehavior** - Executes before mapping
 - Examples: Authentication, Input Validation
 - Order: 1000-1999

3. **IPostMapBehavior** - Executes after mapping
 - Examples: Output Validation, Logging
 - Order: 2000-2999

**Built-in Behaviors**:
- `AuthenticationBehavior` - Adds authentication headers
- `ValidationBehavior` - Validates input/output
- `TimingBehavior` - Measures execution time
- `HttpClientConfigurationBehavior` - Configures HTTP client
- `MessageCaptureBehavior` - Captures messages for debugging

**Pipeline Execution**:
```
Request → WholeRunBehaviors → PreMapBehaviors → Mapping → PostMapBehaviors → Response
```

### 3. Source Resolvers

Source resolvers parse incoming data from various formats.

**Location**: `QuickApiMapper.Application/Resolvers/`

**Implementations**:
- `JsonSourceResolver` - Parses JSON using Newtonsoft.Json with JSONPath support
- `XmlSourceResolver` - Parses XML using XPath
- `GrpcSourceResolver` - Deserializes gRPC messages
- `StaticSourceResolver` - Returns static values

**Interface**:
```csharp
public interface ISourceResolver
{
 Task<string?> ResolveAsync(
 string sourceData,
 string sourcePath,
 MappingContext context);
}
```

### 4. Transformers

Transformers modify field values during mapping.

**Location**:
- `QuickApiMapper.StandardTransformers/` - Built-in transformers
- `QuickApiMapper.CustomTransformers/` - Custom transformers

**Built-in Transformers**:
- `ToUpperTransformer` - Converts to uppercase
- `ToLowerTransformer` - Converts to lowercase
- `ToBooleanTransformer` - Converts to boolean
- `FormatPhoneTransformer` - Formats phone numbers
- `BooleanToYNTransformer` - Converts bool to Y/N

**Custom Transformers**:
Implement `ITransformer` interface:
```csharp
public class MyTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 // Your transformation logic
 return transformed;
 }
}
```

Register in DI:
```csharp
services.AddTransformer<MyTransformer>();
```

### 5. Destination Writers

Destination writers generate output in the target format.

**Location**: `QuickApiMapper.Application/Writers/`

**Implementations**:
- `JsonDestinationWriter` - Generates JSON
- `XmlDestinationWriter` - Generates XML
- `GrpcDestinationWriter` - Serializes gRPC messages

**Interface**:
```csharp
public interface IDestinationWriter
{
 Task<string> WriteAsync(
 Dictionary<string, string> mappedData,
 ApiMappingConfig config);
}
```

### 6. Destination Handlers

Destination handlers deliver the transformed message to the target system.

**Location**: `QuickApiMapper.Application/Destinations/`

**Implementations**:
- `JsonDestinationHandler` - HTTP POST with JSON
- `SoapDestinationHandler` - SOAP envelope over HTTP
- `GrpcDestinationHandler` - gRPC call
- `RabbitMqDestinationHandler` - Publishes to RabbitMQ
- `ServiceBusDestinationHandler` - Sends to Azure Service Bus

**Interface**:
```csharp
public interface IDestinationHandler
{
 Task<MappingResult> HandleAsync(
 string payload,
 ApiMappingConfig config,
 MappingContext context);
}
```

### 7. Persistence Layer

The persistence layer stores integration configurations and provides multi-database support.

**Projects**:
- `QuickApiMapper.Persistence.Abstractions` - Interfaces and models
- `QuickApiMapper.Persistence.PostgreSQL` - PostgreSQL implementation
- `QuickApiMapper.Persistence.SQLite` - SQLite implementation

**Key Patterns**:
- **Repository Pattern** - `IIntegrationMappingRepository`
- **Unit of Work** - `IUnitOfWork` for transactions
- **Provider Pattern** - `IIntegrationMappingProvider` for configuration loading

**Entity Model**:
```
IntegrationMappingEntity
├── FieldMappings (1:N) - FieldMappingEntity
│ └── Transformers (1:N) - TransformerConfigEntity
├── StaticValues (1:N) - StaticValueEntity
├── SoapConfig (1:1) - SoapConfigEntity
│ └── SoapFields (1:N) - SoapFieldEntity
├── GrpcConfig (1:1) - GrpcConfigEntity
├── RabbitMqConfig (1:1) - RabbitMqConfigEntity
└── ServiceBusConfig (1:1) - ServiceBusConfigEntity
```

**Database Providers**:
- SQLite: Default for development, single file database
- PostgreSQL: Recommended for production, full ACID support

### 8. Message Capture System

The message capture system provides debugging and auditing capabilities.

**Projects**:
- `QuickApiMapper.MessageCapture.Abstractions` - Core interfaces
- `QuickApiMapper.MessageCapture.InMemory` - In-memory storage

**Architecture**:
```
Behavior Pipeline
 │
 ▼
MessageCaptureBehavior
 │
 ├─ Capture Input (Before Mapping)
 │ │
 │ ▼
 │ IMessageCaptureProvider.CaptureAsync()
 │ │
 │ ▼
 │ Store CapturedMessage (Input)
 │
 ├─ Execute Mapping
 │
 └─ Capture Output (After Mapping)
 │
 ▼
 IMessageCaptureProvider.CaptureAsync()
 │
 ▼
 Store CapturedMessage (Output)
```

**Provider Implementations**:
- **In-Memory**: `ConcurrentDictionary` storage, 7-day retention
- **Database** (planned): Persistent storage in PostgreSQL/SQLite

**Features**:
- Captures input and output payloads
- Correlation IDs link request/response
- Timestamps and duration tracking
- Error message capture
- Filtering by integration, direction, status, date range

## Design Patterns

### 1. Strategy Pattern

Used for swappable implementations of core components:
- Source resolvers (JSON, XML, gRPC)
- Destination handlers (SOAP, gRPC, RabbitMQ)
- Transformers (ToUpper, FormatPhone, etc.)

### 2. Pipeline Pattern

The behavior pipeline implements the Chain of Responsibility pattern:
```csharp
public async Task<MappingResult> ExecuteAsync(MappingContext context)
{
 var pipeline = _behaviors
 .OrderBy(b => b.Order)
 .Aggregate(
 (Func<MappingContext, Task<MappingResult>>)ExecuteMapping,
 (next, behavior) => ctx => behavior.ExecuteAsync(ctx, next)
 );

 return await pipeline(context);
}
```

### 3. Factory Pattern

`MappingEngineFactory` creates appropriate engine instances based on configuration:
```csharp
public IMappingEngine Create(ApiMappingConfig config)
{
 return config.DestinationType switch
 {
 "SOAP" => new GenericMappingEngine(/* SOAP dependencies */),
 "gRPC" => new GenericMappingEngine(/* gRPC dependencies */),
 _ => new GenericMappingEngine(/* default dependencies */)
 };
}
```

### 4. Repository Pattern

Abstracts data access with clean interfaces:
```csharp
public interface IIntegrationMappingRepository
{
 Task<IntegrationMappingEntity?> GetByIdAsync(Guid id);
 Task<IntegrationMappingEntity?> GetByIntegrationIdAsync(string integrationId);
 Task<IEnumerable<IntegrationMappingEntity>> GetAllAsync();
 Task AddAsync(IntegrationMappingEntity entity);
 Task UpdateAsync(IntegrationMappingEntity entity);
 Task DeleteAsync(Guid id);
}
```

### 5. Provider Pattern

Used for pluggable implementations:
- Configuration providers (Database, File, Cached)
- Message capture providers (In-Memory, Database)
- Message queue providers (In-Memory, RabbitMQ, Service Bus)

### 6. Dependency Injection

All components use constructor injection for testability:
```csharp
public class GenericMappingEngine : IMappingEngine
{
 private readonly ILogger<GenericMappingEngine> _logger;
 private readonly ISourceResolver _sourceResolver;
 private readonly IDestinationWriter _destinationWriter;
 private readonly IDestinationHandler _destinationHandler;

 public GenericMappingEngine(
 ILogger<GenericMappingEngine> logger,
 ISourceResolver sourceResolver,
 IDestinationWriter destinationWriter,
 IDestinationHandler destinationHandler)
 {
 _logger = logger;
 _sourceResolver = sourceResolver;
 _destinationWriter = destinationWriter;
 _destinationHandler = destinationHandler;
 }
}
```

## Request Flow

Here's how a request flows through QuickApiMapper:

### 1. Request Reception
```
HTTP POST /api/map/{integrationId}
 │
 ▼
Runtime Web API (Program.cs)
 │
 ▼
Load Integration Config from Database
```

### 2. Behavior Pipeline - WholeRun Phase
```
TimingBehavior (Order: 100)
 │
 ▼
MessageCaptureBehavior (Order: 200)
 └─ Capture Input
```

### 3. Behavior Pipeline - PreMap Phase
```
AuthenticationBehavior (Order: 1000)
 └─ Add Auth Headers
 │
 ▼
ValidationBehavior (Order: 1100)
 └─ Validate Input Schema
```

### 4. Mapping Execution
```
GenericMappingEngine.ApplyMappingAsync()
 │
 ├─ 1. Parse Source (JsonSourceResolver)
 │ └─ Extract values using JSONPath
 │
 ├─ 2. Apply Field Mappings
 │ └─ For each mapping:
 │ ├─ Resolve source value
 │ ├─ Apply transformers in sequence
 │ └─ Map to destination path
 │
 ├─ 3. Add Static Values
 │ └─ Merge static values into mapped data
 │
 └─ 4. Generate Output (XmlDestinationWriter)
 └─ Build XML structure from mapped data
```

### 5. Behavior Pipeline - PostMap Phase
```
ValidationBehavior (Order: 2100)
 └─ Validate Output Schema
```

### 6. Destination Delivery
```
SoapDestinationHandler.HandleAsync()
 │
 ├─ Build SOAP Envelope
 ├─ Add SOAP Headers
 ├─ Set SOAPAction header
 ├─ POST to destination endpoint
 └─ Return response
```

### 7. Behavior Pipeline - Completion
```
MessageCaptureBehavior
 └─ Capture Output
 │
 ▼
TimingBehavior
 └─ Record Duration
 │
 ▼
Return MappingResult to Client
```

## Extension Points

QuickApiMapper is designed for extensibility:

### 1. Custom Transformers
```csharp
public class CustomTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 // Your logic
 return output;
 }
}

// Register
services.AddTransformer<CustomTransformer>();
```

### 2. Custom Behaviors
```csharp
public class CustomBehavior : IPreMapBehavior
{
 public int Order => 1500;

 public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
 {
 // Pre-mapping logic
 var result = await next(context);
 // Post-execution logic
 return result;
 }
}

// Register
services.AddSingleton<IPreMapBehavior, CustomBehavior>();
```

### 3. Custom Destination Handlers
```csharp
public class CustomHandler : IDestinationHandler
{
 public async Task<MappingResult> HandleAsync(
 string payload,
 ApiMappingConfig config,
 MappingContext context)
 {
 // Send to custom destination
 return new MappingResult { IsSuccess = true };
 }
}

// Register
services.AddScoped<IDestinationHandler, CustomHandler>();
```

### 4. Custom Persistence Provider
```csharp
public class CustomDbContext : DbContext
{
 // Your custom database implementation
}

public class CustomRepository : IIntegrationMappingRepository
{
 // Your custom repository logic
}

// Register
services.AddDbContext<CustomDbContext>();
services.AddScoped<IIntegrationMappingRepository, CustomRepository>();
```

## Performance Considerations

### 1. Caching
- Configuration caching with `CachedConfigurationProvider`
- Transformer instance caching in `TransformerRegistry`
- Database connection pooling

### 2. Async/Await
- All I/O operations are async
- Avoids thread pool starvation
- Supports high concurrency

### 3. Memory Management
- Streaming for large payloads
- Payload truncation for message capture (1MB default)
- Automatic cleanup of in-memory message store

### 4. Database Optimization
- Proper indexing on integration_id and id columns
- Eager loading with Include() to avoid N+1 queries
- Connection pooling

## Security Considerations

### 1. Authentication
- `AuthenticationBehavior` adds configurable auth headers
- Support for Bearer tokens, API keys, Basic auth

### 2. Input Validation
- `ValidationBehavior` validates against schemas
- Prevents injection attacks via safe XML/JSON parsing

### 3. Secrets Management
- Store credentials in appsettings or Azure Key Vault
- Never log sensitive data (passwords, tokens)
- Redact sensitive fields in message capture

### 4. HTTPS
- Always use HTTPS in production
- Configure certificate validation

## Monitoring and Observability

### 1. Logging
- Structured logging with `ILogger<T>`
- Log levels: Trace, Debug, Information, Warning, Error, Critical
- Integration with Seq, Application Insights, etc.

### 2. Health Checks
- Database connectivity checks
- Message queue connectivity checks
- Available at `/health` endpoint

### 3. Metrics
- Request count and duration (via TimingBehavior)
- Success/failure rates
- Message queue depth

### 4. Distributed Tracing
- Correlation IDs for request tracking
- Activity support for distributed tracing
- Integration with OpenTelemetry

## Next Steps

- [Creating Integrations](creating-integrations.md) - Build your first integration
- [Transformers](transformers.md) - Learn about transformers
- [Behaviors](behaviors.md) - Understand the behavior pipeline
- [Deployment](deployment.md) - Deploy to production
