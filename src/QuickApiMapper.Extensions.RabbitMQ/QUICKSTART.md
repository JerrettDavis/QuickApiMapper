# RabbitMQ Extension - Quick Start Guide

This guide will help you get started with the QuickApiMapper RabbitMQ extension in 5 minutes.

## Prerequisites

- .NET 10.0 SDK
- Docker (for running RabbitMQ)
- QuickApiMapper application

## Step 1: Start RabbitMQ

```bash
cd src/QuickApiMapper.Extensions.RabbitMQ
docker-compose up -d
```

Wait for RabbitMQ to start (about 10-15 seconds), then verify it's running:

```bash
docker ps | grep rabbitmq
```

Access the management UI at http://localhost:15672 (username: `guest`, password: `guest`)

## Step 2: Configure Your Application

Add the RabbitMQ configuration to your `appsettings.json`:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "InputQueues": [
      {
        "QueueName": "quickapi.customer.input",
        "DefaultIntegrationName": "CustomerIntegration"
      }
    ]
  },
  "IntegrationMappings": [
    {
      "Name": "CustomerIntegration",
      "Endpoint": "/api/customer",
      "SourceType": "JSON",
      "DestinationType": "JSON",
      "DestinationUrl": "http://localhost:5001/api/customer",
      "EnableInput": true,
      "EnableOutput": true,
      "EnableMessageCapture": true,
      "Mapping": [
        {
          "Source": "$.customerId",
          "Destination": "$.id"
        },
        {
          "Source": "$.firstName",
          "Destination": "$.first_name"
        },
        {
          "Source": "$.lastName",
          "Destination": "$.last_name"
        }
      ]
    }
  ]
}
```

## Step 3: Register RabbitMQ in Program.cs

```csharp
using QuickApiMapper.Extensions.RabbitMQ.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add QuickApiMapper services
builder.Services.AddQuickApiMapper(
    logging => logging.SetMinimumLevel(LogLevel.Information));

builder.Services.AddFileBasedConfiguration();

// Add message capture
builder.Services.AddInMemoryMessageCapture();

// Add RabbitMQ
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddRabbitMqSupport(
    rabbitMqConfig["HostName"]!,
    options =>
    {
        options.InputQueues = rabbitMqConfig.GetSection("InputQueues")
            .Get<List<RabbitMqQueueConfig>>();
    });

var app = builder.Build();
app.Run();
```

## Step 4: Run Your Application

```bash
dotnet run
```

You should see log messages indicating the RabbitMQ consumer has started:

```
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      RabbitMQ consumer initialized for queue quickapi.customer.input with prefetch 10
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Starting RabbitMQ consumer for queue: quickapi.customer.input
```

## Step 5: Send a Test Message

### Option A: Using PowerShell

```powershell
cd src/QuickApiMapper.Extensions.RabbitMQ/scripts
.\publish-test-message.ps1 -MessageType customer
```

### Option B: Using Python

```bash
cd src/QuickApiMapper.Extensions.RabbitMQ/scripts
pip install pika
python publish-test-message.py --type customer
```

### Option C: Using RabbitMQ Management UI

1. Go to http://localhost:15672
2. Login with `guest/guest`
3. Click "Exchanges" > "quickapi.exchange"
4. Scroll to "Publish message"
5. Set routing key to `customer.created`
6. Add header `IntegrationName` = `CustomerIntegration`
7. Set payload:
```json
{
  "customerId": "CUST-1234",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com"
}
```
8. Click "Publish message"

## Step 6: Verify Processing

Check your application logs for processing messages:

```
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Received message from queue quickapi.customer.input, DeliveryTag: 1, CorrelationId: abc123
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Processing message for integration: CustomerIntegration
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Forwarding JSON message to http://localhost:5001/api/customer
info: QuickApiMapper.Extensions.RabbitMQ.Workers.RabbitMqConsumer[0]
      Successfully processed and acknowledged message: 1
```

## Troubleshooting

### RabbitMQ Not Starting

```bash
docker logs quickapi-rabbitmq
```

### Consumer Not Receiving Messages

1. Check queue bindings in RabbitMQ management UI
2. Verify integration name matches configuration
3. Check application logs for connection errors

### Messages Going to Dead-Letter Queue

1. View dead-letter queue: `quickapi.customer.input.dead-letter`
2. Check application logs for processing errors
3. Verify destination URL is accessible

## Next Steps

- Read the full [README.md](README.md) for advanced configuration
- Set up multiple integrations
- Configure SSL/TLS for production
- Implement custom transformers
- Monitor with message capture API

## Clean Up

To stop RabbitMQ:

```bash
docker-compose down
```

To remove all data:

```bash
docker-compose down -v
```
