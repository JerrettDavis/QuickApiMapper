# Azure Service Bus Protocol

Azure Service Bus is a fully managed enterprise message broker. This guide covers Service Bus integration with QuickApiMapper.

## Overview

Azure Service Bus provides reliable cloud messaging with:

- **Queues** - Point-to-point communication
- **Topics/Subscriptions** - Pub/sub messaging
- **Sessions** - Message ordering
- **Dead-letter Queues** - Error handling

## Service Bus Configuration

### Queue Configuration

```json
{
 "name": "Customer to Service Bus Queue",
 "destinationType": "ServiceBus",
 "serviceBusConfig": {
 "connectionString": "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...",
 "queueName": "customer-queue"
 }
}
```

### Topic Configuration

```json
{
 "name": "Customer Events to Service Bus Topic",
 "destinationType": "ServiceBus",
 "serviceBusConfig": {
 "connectionString": "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...",
 "topicName": "customer-events"
 }
}
```

## Connection String

Obtain from Azure Portal:

1. Navigate to Service Bus Namespace
2. Click "Shared access policies"
3. Select or create policy
4. Copy "Primary Connection String"

**Format**:
```
Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=KeyName;SharedAccessKey=KeyValue
```

## Complete Example

### Scenario

Send customer events to Service Bus topic for multiple subscribers.

### Source (JSON)

```json
{
 "customer": {
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "eventType": "Created",
 "timestamp": "2024-01-15T10:30:00Z"
 }
}
```

### Integration Configuration

```json
{
 "name": "Customer Events to Service Bus",
 "integrationId": "customer-events-servicebus",
 "sourceType": "JSON",
 "destinationType": "ServiceBus",
 "serviceBusConfig": {
 "connectionString": "Endpoint=sb://mycompany.servicebus.windows.net/;...",
 "topicName": "customer-events",
 "enableSessions": false
 },
 "fieldMappings": [
 {
 "sourcePath": "$.customer.customerId",
 "destinationPath": "customerId"
 },
 {
 "sourcePath": "$.customer.firstName",
 "destinationPath": "firstName"
 },
 {
 "sourcePath": "$.customer.lastName",
 "destinationPath": "lastName"
 },
 {
 "sourcePath": "$.customer.email",
 "destinationPath": "email"
 },
 {
 "sourcePath": "$.customer.eventType",
 "destinationPath": "eventType"
 },
 {
 "sourcePath": "$.customer.timestamp",
 "destinationPath": "timestamp"
 }
 ],
 "staticValues": [
 {
 "destinationPath": "source",
 "staticValue": "QuickApiMapper"
 }
 ]
}
```

### Sent Message

**Message Body** (JSON):
```json
{
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "eventType": "Created",
 "timestamp": "2024-01-15T10:30:00Z",
 "source": "QuickApiMapper"
}
```

**Message Properties**:
```
MessageId: <generated-guid>
ContentType: application/json
CorrelationId: <integration-correlation-id>
Subject: customer.created
TimeToLive: 14 days
```

## Message Properties

### Standard Properties

```json
{
 "serviceBusConfig": {
 "messageProperties": {
 "contentType": "application/json",
 "correlationId": "{correlationId}",
 "subject": "customer.created",
 "timeToLive": "P14D",
 "messageId": "{guid}",
 "partitionKey": "{customerId}",
 "sessionId": "{customerId}"
 }
 }
}
```

**Properties**:
- **contentType** - MIME type
- **correlationId** - For request/response patterns
- **subject** - Message label (formerly Label)
- **timeToLive** - ISO 8601 duration (e.g., "P14D" = 14 days)
- **messageId** - Unique identifier
- **partitionKey** - For partitioned entities
- **sessionId** - For session-enabled entities

### Custom Properties

```json
{
 "serviceBusConfig": {
 "customProperties": {
 "SourceSystem": "Salesforce",
 "TenantId": "tenant-123",
 "Priority": "High",
 "Region": "US"
 }
 }
}
```

## Message Filtering (Topics)

### Correlation Filters

Subscribers can filter by properties:

**C# Subscription Rule**:
```csharp
var rule = new CorrelationRuleFilter
{
 Subject = "customer.created",
 ApplicationProperties =
 {
 ["Region"] = "US"
 }
};

await adminClient.CreateRuleAsync(topicName, subscriptionName, new CreateRuleOptions
{
 Name = "USCustomersRule",
 Filter = rule
});
```

### SQL Filters

More complex filtering:

```csharp
var sqlFilter = new SqlRuleFilter("Region = 'US' AND Priority = 'High'");

await adminClient.CreateRuleAsync(topicName, subscriptionName, new CreateRuleOptions
{
 Name = "HighPriorityUSCustomers",
 Filter = sqlFilter
});
```

## Sessions

Enable sessions for ordered message processing:

```json
{
 "serviceBusConfig": {
 "enableSessions": true,
 "sessionId": "{customerId}"
 }
}
```

All messages with the same session ID are processed sequentially.

**Use Cases**:
- Order processing (process orders for same customer sequentially)
- State management (maintain session state)
- Message grouping (process related messages together)

## Scheduled Messages

Send messages for future delivery:

```json
{
 "serviceBusConfig": {
 "scheduledEnqueueTime": "2024-01-15T14:00:00Z"
 }
}
```

**Use Cases**:
- Delayed processing
- Scheduled notifications
- Reminder messages

## Consuming Messages

### .NET Consumer

**Queue Consumer**:
```csharp
var client = new ServiceBusClient(connectionString);
var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
{
 AutoCompleteMessages = false,
 MaxConcurrentCalls = 10
});

processor.ProcessMessageAsync += async args =>
{
 var body = args.Message.Body.ToString();
 var customer = JsonSerializer.Deserialize<CustomerEvent>(body);

 Console.WriteLine($"Received: {customer.CustomerId}");

 await args.CompleteMessageAsync(args.Message);
};

processor.ProcessErrorAsync += args =>
{
 Console.WriteLine($"Error: {args.Exception.Message}");
 return Task.CompletedTask;
};

await processor.StartProcessingAsync();
```

**Topic Subscriber**:
```csharp
var client = new ServiceBusClient(connectionString);
var processor = client.CreateProcessor(topicName, subscriptionName);

processor.ProcessMessageAsync += async args =>
{
 var customer = JsonSerializer.Deserialize<CustomerEvent>(args.Message.Body.ToString());
 Console.WriteLine($"Subscription received: {customer.CustomerId}");
 await args.CompleteMessageAsync(args.Message);
};

await processor.StartProcessingAsync();
```

## Error Handling

### Retry Configuration

```json
{
 "serviceBusConfig": {
 "retryPolicy": {
 "maxRetries": 3,
 "delay": "PT1S",
 "maxDelay": "PT30S",
 "mode": "Exponential"
 }
 }
}
```

**Modes**:
- `Exponential` - Exponential backoff
- `Fixed` - Fixed delay between retries

### Dead-Letter Queue

Service Bus automatically moves failed messages to dead-letter queue after max delivery count.

**Access Dead-Letter Queue**:
```csharp
var dlqPath = $"{queueName}/$deadletterqueue";
var receiver = client.CreateReceiver(dlqPath);

await foreach (var message in receiver.ReceiveMessagesAsync())
{
 Console.WriteLine($"Dead-letter: {message.DeadLetterReason}");
 Console.WriteLine($"Description: {message.DeadLetterErrorDescription}");
}
```

## Monitoring

### Message Count

Check queue/topic metrics:

```bash
# Using Azure CLI
az servicebus queue show \
 --resource-group mygroup \
 --namespace-name mynamespace \
 --name customer-queue \
 --query "countDetails"
```

### Dead-Letter Count

```bash
az servicebus queue show \
 --resource-group mygroup \
 --namespace-name mynamespace \
 --name customer-queue \
 --query "countDetails.deadLetterMessageCount"
```

## Best Practices

### 1. Use Partitioned Entities for High Throughput

Enable partitioning when creating queue/topic:

```bash
az servicebus queue create \
 --name customer-queue \
 --namespace-name mynamespace \
 --enable-partitioning true
```

### 2. Set Appropriate TTL

Prevent old messages from accumulating:

```json
{
 "serviceBusConfig": {
 "messageProperties": {
 "timeToLive": "P1D"
 }
 }
}
```

### 3. Use Duplicate Detection

Enable for exactly-once delivery:

```bash
az servicebus queue create \
 --name customer-queue \
 --namespace-name mynamespace \
 --enable-duplicate-detection true \
 --duplicate-detection-history-time-window PT10M
```

Set MessageId consistently:

```json
{
 "serviceBusConfig": {
 "messageProperties": {
 "messageId": "{customerId}-{timestamp}"
 }
 }
}
```

### 4. Monitor Queue Depth

Alert thresholds:
- Warning: > 1000 messages
- Critical: > 5000 messages
- Dead-letter: > 100 messages

### 5. Use Topics for Multiple Subscribers

Use topics instead of multiple queues when:
- Multiple systems need the same data
- Different processing logic per subscriber
- Filtering based on message properties

## Security

### Shared Access Signatures (SAS)

Create scoped access policies:

```bash
az servicebus queue authorization-rule create \
 --resource-group mygroup \
 --namespace-name mynamespace \
 --queue-name customer-queue \
 --name SendOnlyPolicy \
 --rights Send
```

Use in connection string:

```
Endpoint=sb://mynamespace.servicebus.windows.net/;SharedAccessKeyName=SendOnlyPolicy;SharedAccessKey=...
```

### Managed Identity

Use Azure Managed Identity instead of connection strings:

```csharp
var credential = new DefaultAzureCredential();
var client = new ServiceBusClient(
 "mynamespace.servicebus.windows.net",
 credential);
```

## Troubleshooting

### Unauthorized Error

**Error**: `Unauthorized access`

**Solutions**:
1. Verify connection string is correct
2. Check SAS policy has Send permission
3. Ensure namespace name is correct
4. Check if IP firewall is blocking access

### Entity Not Found

**Error**: `MessagingEntityNotFoundException`

**Solutions**:
1. Verify queue/topic exists
2. Check name spelling
3. Ensure namespace is correct

### Message Lock Lost

**Error**: `MessageLockLostException`

**Causes**:
- Processing time exceeds lock duration (default: 60s)

**Solutions**:
1. Increase lock duration
2. Renew lock during processing
3. Complete message faster

### Quota Exceeded

**Error**: `QuotaExceededException`

**Causes**:
- Queue/topic size limit reached
- Too many messages

**Solutions**:
1. Increase entity size
2. Process messages faster
3. Reduce message TTL
4. Check dead-letter queue

## Next Steps

- [RabbitMQ Protocol](rabbitmq.md) - Alternative messaging
- [Creating Integrations](../creating-integrations.md) - Build integrations
