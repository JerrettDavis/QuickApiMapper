# RabbitMQ Protocol

RabbitMQ is a popular message broker for asynchronous messaging. This guide covers RabbitMQ integration with QuickApiMapper.

## Overview

RabbitMQ uses AMQP (Advanced Message Queuing Protocol) for message delivery. QuickApiMapper supports:

- **Publishing** - Send messages to exchanges
- **Exchanges** - Direct, Topic, Fanout, Headers
- **Routing Keys** - Message routing
- **Message Properties** - Headers, content type, priority

## RabbitMQ Configuration

### Basic Setup

```json
{
 "name": "Customer Events to RabbitMQ",
 "destinationType": "RabbitMQ",
 "rabbitMqConfig": {
 "connectionString": "amqp://username:password@rabbitmq.example.com:5672",
 "exchange": "customer-events",
 "routingKey": "customer.created",
 "exchangeType": "topic",
 "durable": true
 }
}
```

### Configuration Properties

- **connectionString** - AMQP connection string
- **exchange** - Exchange name
- **routingKey** - Routing key for messages
- **exchangeType** - direct, topic, fanout, or headers
- **durable** - Persist exchange (default: true)
- **queueName** - Optional queue name (for direct publishing)

## Connection String Format

```
amqp://username:password@host:port/vhost
```

**Examples**:
- `amqp://guest:guest@localhost:5672`
- `amqp://user:pass@rabbit1.example.com:5672/production`
- `amqps://user:pass@rabbit.example.com:5671` (TLS)

## Exchange Types

### Direct Exchange

Routes messages to queues with exact routing key match.

```json
{
 "rabbitMqConfig": {
 "exchange": "orders",
 "exchangeType": "direct",
 "routingKey": "order.created"
 }
}
```

**Use Case**: Simple point-to-point messaging.

### Topic Exchange

Routes messages based on routing key patterns.

```json
{
 "rabbitMqConfig": {
 "exchange": "customer-events",
 "exchangeType": "topic",
 "routingKey": "customer.created.us"
 }
}
```

**Routing Key Patterns**:
- `customer.created.us` - Exact match
- `customer.*.us` - Matches `customer.created.us`, `customer.updated.us`
- `customer.#` - Matches all customer events

**Use Case**: Pub/sub with pattern matching.

### Fanout Exchange

Broadcasts messages to all bound queues (ignores routing key).

```json
{
 "rabbitMqConfig": {
 "exchange": "broadcast",
 "exchangeType": "fanout"
 }
}
```

**Use Case**: Broadcasting to multiple consumers.

### Headers Exchange

Routes based on message headers.

```json
{
 "rabbitMqConfig": {
 "exchange": "headers-exchange",
 "exchangeType": "headers",
 "headers": {
 "x-match": "all",
 "type": "order",
 "priority": "high"
 }
 }
}
```

**Use Case**: Complex routing logic.

## Complete Example

### Scenario

Publish customer created events to RabbitMQ for downstream processing.

### Source (JSON)

```json
{
 "customer": {
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "country": "US",
 "createdAt": "2024-01-15T10:30:00Z"
 }
}
```

### Integration Configuration

```json
{
 "name": "Customer Created to RabbitMQ",
 "integrationId": "customer-created-rabbitmq",
 "sourceType": "JSON",
 "destinationType": "RabbitMQ",
 "rabbitMqConfig": {
 "connectionString": "amqp://quickapi:password@rabbitmq.example.com:5672",
 "exchange": "customer-events",
 "exchangeType": "topic",
 "routingKey": "customer.created.{country}",
 "durable": true,
 "persistent": true
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
 "sourcePath": "$.customer.phone",
 "destinationPath": "phone"
 },
 {
 "sourcePath": "$.customer.country",
 "destinationPath": "country"
 },
 {
 "sourcePath": "$.customer.createdAt",
 "destinationPath": "timestamp"
 }
 ],
 "staticValues": [
 {
 "destinationPath": "eventType",
 "staticValue": "CustomerCreated"
 },
 {
 "destinationPath": "source",
 "staticValue": "QuickApiMapper"
 }
 ]
}
```

### Published Message

**Routing Key**: `customer.created.US`

**Message Body** (JSON):
```json
{
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "country": "US",
 "timestamp": "2024-01-15T10:30:00Z",
 "eventType": "CustomerCreated",
 "source": "QuickApiMapper"
}
```

**Message Properties**:
```
content-type: application/json
delivery-mode: 2 (persistent)
correlation-id: <generated>
timestamp: 1705318200
```

## Message Properties

### Standard Properties

```json
{
 "rabbitMqConfig": {
 "messageProperties": {
 "contentType": "application/json",
 "contentEncoding": "utf-8",
 "priority": 5,
 "expiration": "60000",
 "messageId": "{correlationId}",
 "correlationId": "{correlationId}",
 "replyTo": "response-queue",
 "appId": "quickapimapper"
 }
 }
}
```

**Properties**:
- **contentType** - MIME type (e.g., application/json)
- **contentEncoding** - Encoding (e.g., utf-8)
- **priority** - 0-9 (0 = lowest, 9 = highest)
- **expiration** - TTL in milliseconds
- **messageId** - Unique message ID
- **correlationId** - Request/response correlation
- **replyTo** - Reply queue name
- **appId** - Publishing application name

### Custom Headers

```json
{
 "rabbitMqConfig": {
 "headers": {
 "x-source-system": "salesforce",
 "x-tenant-id": "tenant-123",
 "x-retry-count": "0"
 }
 }
}
```

## Dynamic Routing Keys

Use placeholders for dynamic routing:

```json
{
 "rabbitMqConfig": {
 "routingKey": "customer.{eventType}.{country}"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.eventType",
 "destinationPath": "__routing.eventType"
 },
 {
 "sourcePath": "$.country",
 "destinationPath": "__routing.country"
 }
 ]
}
```

**Result**: `customer.created.US`

## Consuming Messages

To consume messages published by QuickApiMapper:

### .NET Consumer

```csharp
var factory = new ConnectionFactory
{
 Uri = new Uri("amqp://user:pass@rabbitmq.example.com:5672")
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare("customer-events", "topic", durable: true);
channel.QueueDeclare("customer-processor", durable: true);
channel.QueueBind("customer-processor", "customer-events", "customer.created.*");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
 var body = ea.Body.ToArray();
 var message = Encoding.UTF8.GetString(body);
 var customer = JsonSerializer.Deserialize<CustomerEvent>(message);

 Console.WriteLine($"Received: {customer.CustomerId}");

 channel.BasicAck(ea.DeliveryTag, false);
};

channel.BasicConsume("customer-processor", false, consumer);
```

### Python Consumer

```python
import pika
import json

connection = pika.BlockingConnection(
 pika.URLParameters('amqp://user:pass@rabbitmq.example.com:5672'))
channel = connection.channel()

channel.exchange_declare(exchange='customer-events', exchange_type='topic', durable=True)
channel.queue_declare(queue='customer-processor', durable=True)
channel.queue_bind(queue='customer-processor', exchange='customer-events', routing_key='customer.created.*')

def callback(ch, method, properties, body):
 customer = json.loads(body)
 print(f"Received: {customer['customerId']}")
 ch.basic_ack(delivery_tag=method.delivery_tag)

channel.basic_consume(queue='customer-processor', on_message_callback=callback)
channel.start_consuming()
```

## Error Handling

### Retry Configuration

```json
{
 "rabbitMqConfig": {
 "retryPolicy": {
 "maxRetries": 3,
 "retryDelayMs": 1000,
 "retryExchange": "customer-events-retry",
 "deadLetterExchange": "customer-events-dlx"
 }
 }
}
```

### Dead Letter Exchange

Configure DLX for failed messages:

```json
{
 "rabbitMqConfig": {
 "deadLetterExchange": "customer-events-dlx",
 "deadLetterRoutingKey": "customer.failed"
 }
}
```

## Best Practices

### 1. Use Durable Exchanges and Queues

Ensure messages survive restarts:

```json
{
 "rabbitMqConfig": {
 "durable": true,
 "persistent": true
 }
}
```

### 2. Set Appropriate TTL

Prevent message buildup:

```json
{
 "rabbitMqConfig": {
 "messageProperties": {
 "expiration": "3600000"
 }
 }
}
```

### 3. Use Meaningful Routing Keys

Follow a clear convention:

```
{entity}.{operation}.{region}
```

Examples:
- `customer.created.us`
- `order.updated.eu`
- `product.deleted.asia`

### 4. Monitor Queue Depth

Alert on queue buildup:
- Normal: < 1000 messages
- Warning: 1000-5000 messages
- Critical: > 5000 messages

### 5. Implement Idempotency

Handle duplicate messages:

```json
{
 "rabbitMqConfig": {
 "messageProperties": {
 "messageId": "{correlationId}"
 }
 }
}
```

Store processed message IDs to detect duplicates.

## Troubleshooting

### Connection Failed

**Error**: `Connection refused` or `Authentication failed`

**Solutions**:
1. Verify RabbitMQ is running
2. Check connection string
3. Verify credentials
4. Check firewall rules

### Exchange Not Found

**Error**: `NOT_FOUND - no exchange 'xyz'`

**Solutions**:
1. Ensure exchange is declared
2. Check exchange name spelling
3. Verify permissions

### Messages Not Routed

**Cause**: No queues bound to exchange/routing key

**Solutions**:
1. Check queue bindings
2. Verify routing key matches binding pattern
3. Enable return handling:

```json
{
 "rabbitMqConfig": {
 "mandatory": true,
 "handleReturns": true
 }
}
```

## Next Steps

- [Azure Service Bus](service-bus.md) - Cloud messaging
- [Creating Integrations](../creating-integrations.md) - Build integrations
