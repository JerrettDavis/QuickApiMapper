# QuickApiMapper RabbitMQ Extension

This extension adds RabbitMQ support to QuickApiMapper, enabling asynchronous message-driven integration patterns. Messages received from RabbitMQ queues are automatically processed through the QuickApiMapper pipeline, transformed according to configured mappings, and forwarded to destination systems.

## Features

- **Automatic Message Processing**: Consume messages from RabbitMQ queues and process them through QuickApiMapper
- **Dead-Letter Queue Support**: Failed messages are automatically sent to a dead-letter queue for manual inspection
- **Message Capture Integration**: Full integration with QuickApiMapper's message capture system for audit trails
- **Flexible Integration Mapping**: Support for multiple integration configurations via message properties or routing keys
- **JSON and SOAP Support**: Process both JSON and SOAP/XML message formats
- **Correlation Tracking**: Track messages end-to-end using RabbitMQ correlation IDs
- **Production-Ready Error Handling**: Comprehensive error handling with logging and dead-letter queue support

## Installation

Add the RabbitMQ extension to your QuickApiMapper project:

```bash
dotnet add package QuickApiMapper.Extensions.RabbitMQ
```

## Configuration

### 1. Add RabbitMQ to appsettings.json

```json
{
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
        "QueueName": "quickapi.customer.input",
        "ExchangeName": "quickapi.exchange",
        "RoutingKey": "customer.created",
        "DefaultIntegrationName": "CustomerIntegration"
      },
      {
        "QueueName": "quickapi.order.input",
        "ExchangeName": "quickapi.exchange",
        "RoutingKey": "order.created",
        "DefaultIntegrationName": "OrderIntegration"
      }
    ]
  }
}
```

### 2. Register RabbitMQ Services in Program.cs

```csharp
using QuickApiMapper.Extensions.RabbitMQ.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add QuickApiMapper with all required services
builder.Services.AddQuickApiMapper(
    logging => logging.SetMinimumLevel(LogLevel.Information),
    transformerDirectory: "Transformers");

// Add configuration provider
builder.Services.AddFileBasedConfiguration();

// Add message capture
builder.Services.AddInMemoryMessageCapture(options =>
{
    options.MaxPayloadSizeKB = 2048;
    options.RetentionPeriod = TimeSpan.FromDays(7);
});

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

var app = builder.Build();
app.Run();
```

## Message Format

### Option 1: Specify Integration via Message Property

When publishing messages to RabbitMQ, include an `IntegrationName` header:

```csharp
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

var properties = channel.CreateBasicProperties();
properties.Headers = new Dictionary<string, object>
{
    ["IntegrationName"] = "CustomerIntegration"
};
properties.CorrelationId = Guid.NewGuid().ToString();

var body = Encoding.UTF8.GetBytes(jsonPayload);
channel.BasicPublish(
    exchange: "quickapi.exchange",
    routingKey: "customer.created",
    basicProperties: properties,
    body: body);
```

### Option 2: Use Routing Key as Integration Name

If the routing key matches an integration name, it will be used automatically:

```csharp
channel.BasicPublish(
    exchange: "quickapi.exchange",
    routingKey: "CustomerIntegration",  // This will be used as integration name
    basicProperties: properties,
    body: body);
```

### Option 3: Use Default Integration Name

Configure a default integration name in the queue configuration:

```json
{
  "QueueName": "quickapi.customer.input",
  "DefaultIntegrationName": "CustomerIntegration"
}
```

## Message Examples

### JSON Message Example

```json
{
  "customerId": "12345",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phoneNumber": "+1-555-0123"
}
```

### SOAP Message Example

```xml
<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <CreateCustomer xmlns="http://example.com/customer">
      <CustomerId>12345</CustomerId>
      <FirstName>John</FirstName>
      <LastName>Doe</LastName>
      <Email>john.doe@example.com</Email>
      <PhoneNumber>+1-555-0123</PhoneNumber>
    </CreateCustomer>
  </soap:Body>
</soap:Envelope>
```

## Integration Configuration

Define your integration mappings in `appsettings.json`:

```json
{
  "IntegrationMappings": [
    {
      "Name": "CustomerIntegration",
      "Endpoint": "/api/customer",
      "SourceType": "JSON",
      "DestinationType": "SOAP",
      "DestinationUrl": "https://example.com/soap/customer",
      "EnableInput": true,
      "EnableOutput": true,
      "EnableMessageCapture": true,
      "StaticValues": {
        "TnsNamespace": "http://example.com/customer",
        "SystemId": "QuickAPI"
      },
      "Mapping": [
        {
          "Source": "$.customerId",
          "Destination": "//tns:CustomerId",
          "Transformers": []
        },
        {
          "Source": "$.firstName",
          "Destination": "//tns:FirstName",
          "Transformers": []
        },
        {
          "Source": "$.lastName",
          "Destination": "//tns:LastName",
          "Transformers": []
        }
      ]
    }
  ]
}
```

## Testing with RabbitMQ

### 1. Start RabbitMQ with Docker

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

Access the management UI at http://localhost:15672 (guest/guest)

### 2. Publish Test Messages

#### Using PowerShell

Create a file `publish-test-message.ps1`:

```powershell
param(
    [string]$IntegrationName = "CustomerIntegration",
    [string]$Exchange = "quickapi.exchange",
    [string]$RoutingKey = "customer.created"
)

# Install RabbitMQ .NET client if not already installed
# dotnet add package RabbitMQ.Client

$script = @"
using System;
using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declare exchange
channel.ExchangeDeclare("$Exchange", "direct", durable: true);

// Create message
var message = @"{
  ""customerId"": ""12345"",
  ""firstName"": ""John"",
  ""lastName"": ""Doe"",
  ""email"": ""john.doe@example.com"",
  ""phoneNumber"": ""+1-555-0123""
}";

// Set properties
var properties = channel.CreateBasicProperties();
properties.Headers = new Dictionary<string, object>
{
    ["IntegrationName"] = "$IntegrationName"
};
properties.CorrelationId = Guid.NewGuid().ToString();
properties.Persistent = true;

// Publish
var body = Encoding.UTF8.GetBytes(message);
channel.BasicPublish(
    exchange: "$Exchange",
    routingKey: "$RoutingKey",
    basicProperties: properties,
    body: body);

Console.WriteLine("Message published successfully!");
"@

# Create temp C# file
$tempFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.csx'
Set-Content -Path $tempFile -Value $script

# Run with dotnet script
dotnet script $tempFile

# Clean up
Remove-Item $tempFile
```

Run it:
```powershell
.\publish-test-message.ps1 -IntegrationName "CustomerIntegration"
```

#### Using curl and RabbitMQ HTTP API

```bash
curl -u guest:guest -X POST http://localhost:15672/api/exchanges/%2F/quickapi.exchange/publish \
  -H "Content-Type: application/json" \
  -d '{
    "properties": {
      "headers": {
        "IntegrationName": "CustomerIntegration"
      },
      "correlation_id": "test-123"
    },
    "routing_key": "customer.created",
    "payload": "{\"customerId\":\"12345\",\"firstName\":\"John\",\"lastName\":\"Doe\"}",
    "payload_encoding": "string"
  }'
```

### 3. Monitor Message Processing

Check the application logs for processing details:

```
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Received message from queue quickapi.customer.input, DeliveryTag: 1, CorrelationId: test-123
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Processing message for integration: CustomerIntegration
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Forwarding SOAP message to https://example.com/soap/customer
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Successfully processed and acknowledged message: 1
```

## Error Handling

### Dead-Letter Queue

Failed messages are automatically sent to a dead-letter queue for inspection:

- **Main Queue**: `quickapi.customer.input`
- **Dead-Letter Exchange**: `quickapi.customer.input.dlx`
- **Dead-Letter Queue**: `quickapi.customer.input.dead-letter`

You can inspect failed messages in the RabbitMQ management UI or programmatically consume from the dead-letter queue.

### Message Capture

All messages (successful and failed) are captured if message capture is enabled:

```csharp
// Query captured messages
var messageCaptureProvider = serviceProvider.GetRequiredService<IMessageCaptureProvider>();
var messages = await messageCaptureProvider.QueryAsync(new MessageFilter
{
    IntegrationName = "CustomerIntegration",
    Status = MessageStatus.Failed,
    PageSize = 50
});
```

## Advanced Configuration

### Multiple Queues

Process messages from multiple queues with different integrations:

```json
{
  "InputQueues": [
    {
      "QueueName": "quickapi.customer.input",
      "DefaultIntegrationName": "CustomerIntegration"
    },
    {
      "QueueName": "quickapi.order.input",
      "DefaultIntegrationName": "OrderIntegration"
    },
    {
      "QueueName": "quickapi.invoice.input",
      "DefaultIntegrationName": "InvoiceIntegration"
    }
  ]
}
```

### SSL/TLS Connection

For production environments, enable SSL:

```json
{
  "RabbitMQ": {
    "HostName": "rabbitmq.production.com",
    "Port": 5671,
    "UseSsl": true,
    "UserName": "production-user",
    "Password": "secure-password"
  }
}
```

### Prefetch Count Tuning

Adjust the prefetch count based on message processing time:

```json
{
  "PrefetchCount": 5  // Lower for slow processing, higher for fast processing
}
```

## Monitoring and Observability

The RabbitMQ extension integrates with .NET logging and can be monitored through:

1. **Application Logs**: Detailed processing logs at various levels
2. **Message Capture**: Query processing history and statistics
3. **RabbitMQ Management UI**: Monitor queue depths and consumer status
4. **Dead-Letter Queue**: Track failed messages

### Metrics to Monitor

- Queue depth (messages waiting)
- Message processing rate
- Dead-letter queue depth
- Consumer count and status
- Message capture statistics

## Troubleshooting

### Consumer Not Starting

**Problem**: Consumer doesn't start or connect to RabbitMQ

**Solutions**:
- Check RabbitMQ is running: `docker ps | grep rabbitmq`
- Verify connection settings in appsettings.json
- Check network connectivity to RabbitMQ host
- Review application logs for connection errors

### Messages Going to Dead-Letter Queue

**Problem**: All messages end up in the dead-letter queue

**Solutions**:
- Check integration configuration exists for the specified integration name
- Verify message format matches integration source type
- Review application logs for specific error messages
- Check destination URL is accessible

### Integration Not Found

**Problem**: Error "Integration 'X' not found in configuration"

**Solutions**:
- Verify integration is defined in appsettings.json or database
- Check integration name spelling in message headers or routing key
- Ensure configuration provider is properly registered

### Message Capture Not Working

**Problem**: Messages are processed but not captured

**Solutions**:
- Verify message capture provider is registered: `services.AddInMemoryMessageCapture()`
- Check `EnableMessageCapture` is `true` in integration configuration
- Review logs for message capture errors (non-fatal)

## Performance Considerations

1. **Prefetch Count**: Higher values increase throughput but consume more memory
2. **Message Capture**: Can impact performance for high-volume scenarios
3. **Destination Latency**: Slow destination APIs will limit throughput
4. **Queue Durability**: Durable queues are slower but survive restarts

## Best Practices

1. **Use Correlation IDs**: Always set correlation IDs for end-to-end tracking
2. **Monitor Dead-Letter Queues**: Regularly inspect and process failed messages
3. **Set Appropriate Timeouts**: Configure HTTP client timeouts for destination calls
4. **Use Message TTL**: Set time-to-live for messages to prevent queue buildup
5. **Enable Message Capture**: Essential for audit trails and debugging
6. **Use Explicit Integration Names**: Specify via message headers for clarity
7. **Test with Small Prefetch**: Start with low prefetch and tune based on performance

## License

This extension is part of QuickApiMapper and follows the same license.
