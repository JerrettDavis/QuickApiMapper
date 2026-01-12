# RabbitMQ Extension - Complete Usage Example

This document shows a complete, working example of using the RabbitMQ extension in a QuickApiMapper application.

## Scenario

We'll build a customer data integration that:
1. Receives customer data from a legacy system via RabbitMQ (JSON format)
2. Transforms it using QuickApiMapper field mappings
3. Forwards it to a modern API (different JSON schema)
4. Captures all messages for audit trail

## Directory Structure

```
MyQuickApiMapperApp/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── MyQuickApiMapperApp.csproj
```

## Step 1: Project Setup

### MyQuickApiMapperApp.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" />
    <ProjectReference Include="..\QuickApiMapper.Application\QuickApiMapper.Application.csproj" />
    <ProjectReference Include="..\QuickApiMapper.Extensions.RabbitMQ\QuickApiMapper.Extensions.RabbitMQ.csproj" />
    <ProjectReference Include="..\QuickApiMapper.MessageCapture.InMemory\QuickApiMapper.MessageCapture.InMemory.csproj" />
  </ItemGroup>
</Project>
```

## Step 2: Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "QuickApiMapper": "Debug",
      "QuickApiMapper.Extensions.RabbitMQ": "Information"
    }
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "guest",
    "Password": "guest",
    "UseSsl": false,
    "PrefetchCount": 10,
    "InputQueues": [
      {
        "QueueName": "customer.legacy.input",
        "ExchangeName": "legacy.system.events",
        "RoutingKey": "customer.created",
        "DefaultIntegrationName": "LegacyCustomerIntegration"
      }
    ]
  },
  "IntegrationMappings": [
    {
      "Name": "LegacyCustomerIntegration",
      "Endpoint": "/api/legacy/customer",
      "SourceType": "JSON",
      "DestinationType": "JSON",
      "DestinationUrl": "https://api.moderncrm.com/v2/customers",
      "EnableInput": true,
      "EnableOutput": true,
      "EnableMessageCapture": true,
      "StaticValues": {
        "DataSource": "LegacySystem",
        "Version": "1.0",
        "DefaultCountry": "USA"
      },
      "Mapping": [
        {
          "Source": "$.legacy_customer_id",
          "Destination": "$.customerId",
          "Transformers": []
        },
        {
          "Source": "$.first_name",
          "Destination": "$.firstName",
          "Transformers": [
            {
              "Name": "TitleCase",
              "Parameters": {}
            }
          ]
        },
        {
          "Source": "$.last_name",
          "Destination": "$.lastName",
          "Transformers": [
            {
              "Name": "TitleCase",
              "Parameters": {}
            }
          ]
        },
        {
          "Source": "$.email_address",
          "Destination": "$.email",
          "Transformers": [
            {
              "Name": "ToLower",
              "Parameters": {}
            }
          ]
        },
        {
          "Source": "$.phone",
          "Destination": "$.phoneNumber",
          "Transformers": []
        },
        {
          "Source": "$.street_address",
          "Destination": "$.address.street",
          "Transformers": []
        },
        {
          "Source": "$.city",
          "Destination": "$.address.city",
          "Transformers": []
        },
        {
          "Source": "$.state",
          "Destination": "$.address.state",
          "Transformers": [
            {
              "Name": "ToUpper",
              "Parameters": {}
            }
          ]
        },
        {
          "Source": "$.zip",
          "Destination": "$.address.postalCode",
          "Transformers": []
        },
        {
          "Source": "$$.DefaultCountry",
          "Destination": "$.address.country",
          "Transformers": []
        },
        {
          "Source": "$.created_date",
          "Destination": "$.createdAt",
          "Transformers": [
            {
              "Name": "DateTimeFormat",
              "Parameters": {
                "Format": "yyyy-MM-ddTHH:mm:ssZ"
              }
            }
          ]
        },
        {
          "Source": "$$.DataSource",
          "Destination": "$.metadata.source",
          "Transformers": []
        },
        {
          "Source": "$$.Version",
          "Destination": "$.metadata.integrationVersion",
          "Transformers": []
        }
      ]
    }
  ],
  "MessageCapture": {
    "MaxPayloadSizeKB": 2048,
    "RetentionPeriod": "7.00:00:00"
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "QuickApiMapper": "Trace"
    }
  },
  "IntegrationMappings": [
    {
      "Name": "LegacyCustomerIntegration",
      "DestinationUrl": "http://localhost:5001/api/customers",
      "EnableOutput": false
    }
  ]
}
```

## Step 3: Application Code

### Program.cs

```csharp
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Extensions.RabbitMQ.Extensions;
using QuickApiMapper.MessageCapture.InMemory.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add QuickApiMapper with standard transformers
builder.Services.AddQuickApiMapper(
    logging => logging.SetMinimumLevel(LogLevel.Information),
    transformerDirectory: "Transformers");

// Add file-based configuration provider
builder.Services.AddFileBasedConfiguration();

// Optional: Add caching for better performance
builder.Services.AddCachedConfiguration(TimeSpan.FromMinutes(5));

// Add message capture for audit trail
builder.Services.AddInMemoryMessageCapture(options =>
{
    var captureConfig = builder.Configuration.GetSection("MessageCapture");
    options.MaxPayloadSizeKB = captureConfig.GetValue<int>("MaxPayloadSizeKB", 2048);
    options.RetentionPeriod = captureConfig.GetValue<TimeSpan>("RetentionPeriod", TimeSpan.FromDays(7));
});

// Register message capture behavior
builder.Services.AddSingleton<QuickApiMapper.Contracts.IWholeRunBehavior,
    QuickApiMapper.Behaviors.MessageCaptureBehavior>();

// Add RabbitMQ support
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddRabbitMqSupport(
    rabbitMqConfig["HostName"]!,
    options =>
    {
        options.Port = rabbitMqConfig.GetValue<int>("Port");
        options.VirtualHost = rabbitMqConfig["VirtualHost"]!;
        options.UserName = rabbitMqConfig["UserName"]!;
        options.Password = rabbitMqConfig["Password"]!;
        options.UseSsl = rabbitMqConfig.GetValue<bool>("UseSsl");
        options.PrefetchCount = rabbitMqConfig.GetValue<int>("PrefetchCount");
        options.InputQueues = rabbitMqConfig.GetSection("InputQueues")
            .Get<List<RabbitMqQueueConfig>>();
    });

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("rabbitmq", () =>
    {
        // Simple health check - could be enhanced to check connection
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    });

var app = builder.Build();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("QuickApiMapper RabbitMQ Integration Starting...");

// Map health check endpoints
app.MapDefaultEndpoints();

// Add a simple status endpoint
app.MapGet("/", () => new
{
    Status = "Running",
    Service = "QuickApiMapper RabbitMQ Integration",
    Version = "1.0.0"
});

logger.LogInformation("Application started successfully");

app.Run();
```

## Step 4: Testing

### Start RabbitMQ

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

### Start the Application

```bash
dotnet run
```

You should see:

```
info: MyQuickApiMapperApp.Program[0]
      QuickApiMapper RabbitMQ Integration Starting...
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      RabbitMQ consumer initialized for queue customer.legacy.input with prefetch 10
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Starting RabbitMQ consumer for queue: customer.legacy.input
info: MyQuickApiMapperApp.Program[0]
      Application started successfully
```

### Publish a Test Message

Create a file `test-customer.json`:

```json
{
  "legacy_customer_id": "LEGACY-12345",
  "first_name": "john",
  "last_name": "DOE",
  "email_address": "JOHN.DOE@EXAMPLE.COM",
  "phone": "+1-555-0123",
  "street_address": "123 Main St",
  "city": "Springfield",
  "state": "il",
  "zip": "62701",
  "created_date": "2024-01-15T10:30:00"
}
```

Using PowerShell:

```powershell
# Install pika for Python script or use C# script
$message = Get-Content test-customer.json -Raw

# Using RabbitMQ HTTP API
curl -u guest:guest -X POST http://localhost:15672/api/exchanges/%2F/legacy.system.events/publish `
  -H "Content-Type: application/json" `
  -d "{
    'properties': {
      'headers': {
        'IntegrationName': 'LegacyCustomerIntegration'
      },
      'correlation_id': 'test-123'
    },
    'routing_key': 'customer.created',
    'payload': '$($message -replace "'", "\'" -replace '"', '\"')',
    'payload_encoding': 'string'
  }"
```

### Expected Output

The message will be transformed to:

```json
{
  "customerId": "LEGACY-12345",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+1-555-0123",
  "address": {
    "street": "123 Main St",
    "city": "Springfield",
    "state": "IL",
    "postalCode": "62701",
    "country": "USA"
  },
  "createdAt": "2024-01-15T10:30:00Z",
  "metadata": {
    "source": "LegacySystem",
    "integrationVersion": "1.0"
  }
}
```

And forwarded to `https://api.moderncrm.com/v2/customers`.

### Verify in Logs

```
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Received message from queue customer.legacy.input, DeliveryTag: 1, CorrelationId: test-123
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Processing message for integration: LegacyCustomerIntegration
debug: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Detected source type: JSON
debug: QuickApiMapper.MessageCapture.Abstractions[0]
      Captured input message for correlation ID test-123
info: QuickApiMapper.Application.Core.GenericMappingEngine[0]
      Processed 13 mappings, 13 successful
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Forwarding JSON message to https://api.moderncrm.com/v2/customers
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Successfully forwarded message to destination, response: 200
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Message processing completed in 234ms
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Successfully processed and acknowledged message: 1
```

## Step 5: Query Captured Messages

You can query captured messages using the IMessageCaptureProvider:

```csharp
app.MapGet("/api/messages", async (IMessageCaptureProvider captureProvider) =>
{
    var filter = new MessageFilter
    {
        IntegrationName = "LegacyCustomerIntegration",
        StartDate = DateTime.UtcNow.AddDays(-1),
        PageSize = 50
    };

    var messages = await captureProvider.QueryAsync(filter);
    return Results.Ok(messages);
});

app.MapGet("/api/messages/stats", async (IMessageCaptureProvider captureProvider) =>
{
    var stats = await captureProvider.GetStatisticsAsync(
        Guid.Empty, // Integration ID
        DateTime.UtcNow.AddDays(-7),
        DateTime.UtcNow);

    return Results.Ok(new
    {
        stats.TotalMessages,
        stats.SuccessfulMessages,
        stats.FailedMessages,
        stats.SuccessRate,
        AverageDurationMs = stats.AverageDuration?.TotalMilliseconds
    });
});
```

## Error Scenarios

### Scenario 1: Destination API is Down

**What Happens:**
1. Message is received and processed
2. Transformation succeeds
3. HTTP call to destination fails
4. Message is captured with failed status
5. Message is sent to dead-letter queue
6. Error is logged with correlation ID

**Dead-Letter Queue:**
- Queue Name: `customer.legacy.input.dead-letter`
- Contains original message
- Can be inspected via RabbitMQ management UI

### Scenario 2: Invalid Integration Name

**What Happens:**
1. Message is received with invalid integration name
2. Configuration lookup fails
3. Error is logged
4. Message is sent to dead-letter queue

### Scenario 3: Malformed JSON

**What Happens:**
1. Message is received
2. JSON parsing fails
3. Error is logged
4. Message is sent to dead-letter queue

## Production Deployment

### Environment Variables

```bash
export RabbitMQ__HostName=rabbitmq.production.com
export RabbitMQ__Port=5671
export RabbitMQ__UseSsl=true
export RabbitMQ__UserName=prod-user
export RabbitMQ__Password=secure-password
export IntegrationMappings__0__DestinationUrl=https://api.prod.com/v2/customers
```

### Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyQuickApiMapperApp.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyQuickApiMapperApp.dll"]
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: quickapi-rabbitmq
spec:
  replicas: 3
  selector:
    matchLabels:
      app: quickapi-rabbitmq
  template:
    metadata:
      labels:
        app: quickapi-rabbitmq
    spec:
      containers:
      - name: quickapi
        image: myregistry/quickapi-rabbitmq:1.0
        env:
        - name: RabbitMQ__HostName
          value: rabbitmq-service
        - name: RabbitMQ__UserName
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: username
        - name: RabbitMQ__Password
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: password
```

## Monitoring

### Key Metrics to Monitor

1. **Queue Depth**: Messages waiting in queue
2. **Processing Rate**: Messages/second
3. **Error Rate**: Failed messages/total messages
4. **Dead-Letter Queue Depth**: Failed messages accumulating
5. **Processing Duration**: Average time per message

### Alerts to Configure

1. Queue depth > 1000 for 5 minutes
2. Error rate > 5% for 10 minutes
3. Dead-letter queue depth > 100
4. Processing duration > 5 seconds (95th percentile)
5. Consumer disconnected for > 1 minute

## Conclusion

This example demonstrates a complete, production-ready integration using the QuickApiMapper RabbitMQ extension. Key features:

- ✅ Full configuration management
- ✅ Comprehensive error handling
- ✅ Message capture/audit trail
- ✅ Dead-letter queue support
- ✅ Correlation tracking
- ✅ Production deployment patterns
- ✅ Monitoring and alerting guidelines
