# Message Capture

Message capture provides debugging and auditing capabilities by capturing input and output messages as they flow through integrations. This guide covers configuration, usage, and best practices.

## Overview

The message capture system records:
- **Input messages** - Incoming requests before transformation
- **Output messages** - Transformed data sent to destinations
- **Metadata** - Timestamps, duration, correlation IDs, errors
- **Status** - Success or failure information

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│ Incoming Request │
└──────────────────────────────────────────────────────────┘
 │
 ▼
┌──────────────────────────────────────────────────────────┐
│ MessageCaptureBehavior (Order: 200) │
│ ┌────────────────────────────────────────────────────┐ │
│ │ 1. Generate Correlation ID │ │
│ │ 2. Capture Input Message │ │
│ │ ├─ Timestamp │ │
│ │ ├─ Payload │ │
│ │ └─ Metadata │ │
│ └────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
 │
 ▼
 ┌──────────────────┐
 │ Execute Mapping │
 └──────────────────┘
 │
 ▼
┌──────────────────────────────────────────────────────────┐
│ MessageCaptureBehavior (Continued) │
│ ┌────────────────────────────────────────────────────┐ │
│ │ 3. Capture Output Message │ │
│ │ ├─ Timestamp │ │
│ │ ├─ Transformed Payload │ │
│ │ ├─ Duration │ │
│ │ ├─ Status (Success/Failed) │ │
│ │ ├─ Error Message (if failed) │ │
│ │ └─ Same Correlation ID │ │
│ └────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
 │
 ▼
 ┌──────────────────┐
 │ IMessageCapture │
 │ Provider │
 └──────────────────┘
 │
 ┌──────────────┴──────────────┐
 ▼ ▼
 ┌───────────────┐ ┌──────────────────┐
 │ In-Memory │ │ Database │
 │ Provider │ │ Provider │
 └───────────────┘ └──────────────────┘
```

## Message Capture Providers

QuickApiMapper supports multiple storage backends through a provider pattern.

### In-Memory Provider (Default)

Best for development and low-traffic scenarios.

**Features**:
- Fast, no external dependencies
- Automatic cleanup of old messages
- Configurable retention period
- Thread-safe using `ConcurrentDictionary`

**Limitations**:
- Messages lost on restart
- Limited by available memory
- Not suitable for high-traffic production

**Configuration**:
```csharp
builder.Services.AddInMemoryMessageCapture(options =>
{
 options.MaxPayloadSizeKB = 1024; // 1 MB max
 options.RetentionPeriod = TimeSpan.FromDays(7); // Keep for 7 days
 options.MaxMessages = 10000; // Max messages to store
});
```

### Database Provider

Best for production environments requiring persistent storage.

**Features**:
- Persistent storage (survives restarts)
- Unlimited retention (based on disk space)
- Queryable with SQL
- Supports PostgreSQL and SQLite

**Configuration**:
```csharp
builder.Services.AddDatabaseMessageCapture(options =>
{
 options.ConnectionString = configuration.GetConnectionString("QuickApiMapper");
 options.MaxPayloadSizeKB = 2048; // 2 MB max
 options.CompressPayloads = true; // Compress large payloads
 options.RetentionDays = 30; // Auto-purge after 30 days
});
```

**Database Schema**:
```sql
CREATE TABLE captured_messages (
 id VARCHAR(50) PRIMARY KEY,
 integration_id UUID NOT NULL,
 integration_name VARCHAR(200),
 direction VARCHAR(10) NOT NULL, -- 'Input' or 'Output'
 payload TEXT NOT NULL,
 is_truncated BOOLEAN DEFAULT FALSE,
 status VARCHAR(20) NOT NULL, -- 'Success', 'Failed', 'Processing'
 error_message TEXT,
 duration_ms BIGINT,
 correlation_id VARCHAR(50) NOT NULL,
 timestamp TIMESTAMP NOT NULL,
 metadata JSONB,
 FOREIGN KEY (integration_id) REFERENCES integration_mappings(id)
);

CREATE INDEX idx_captured_messages_integration ON captured_messages(integration_id);
CREATE INDEX idx_captured_messages_correlation ON captured_messages(correlation_id);
CREATE INDEX idx_captured_messages_timestamp ON captured_messages(timestamp);
CREATE INDEX idx_captured_messages_direction ON captured_messages(direction);
```

## Captured Message Model

Each captured message contains:

```csharp
public class CapturedMessage
{
 // Unique message ID
 public string Id { get; set; }

 // Integration that processed the message
 public Guid IntegrationId { get; set; }
 public string? IntegrationName { get; set; }

 // Direction: Input (request) or Output (response)
 public MessageDirection Direction { get; set; }

 // The actual payload (JSON, XML, etc.)
 public string Payload { get; set; }

 // True if payload was truncated due to size limits
 public bool IsTruncated { get; set; }

 // Processing status
 public MessageStatus Status { get; set; }

 // Error message if Status = Failed
 public string? ErrorMessage { get; set; }

 // How long the mapping took (Output messages only)
 public TimeSpan? Duration { get; set; }

 // Links Input and Output messages
 public string CorrelationId { get; set; }

 // When the message was captured
 public DateTime Timestamp { get; set; }

 // Additional metadata
 public Dictionary<string, string>? Metadata { get; set; }
}

public enum MessageDirection
{
 Input,
 Output
}

public enum MessageStatus
{
 Processing,
 Success,
 Failed
}
```

## Viewing Captured Messages

### Via Web Designer

1. **Navigate to Integration**:
 - Open Web Designer at `http://localhost:5173`
 - Click "Integrations"
 - Select an integration

2. **View Message History**:
 - Click "Message History" tab
 - View list of captured messages

3. **Filter Messages**:
 - **Date Range**: Select start and end dates
 - **Direction**: Filter by Input or Output
 - **Status**: Filter by Success or Failed
 - **Search**: Search by correlation ID or payload content

4. **View Message Details**:
 - Click on a message row
 - View full payload
 - See metadata and duration
 - Copy payload for testing

5. **Linked Messages**:
 - Input and Output messages with the same correlation ID are linked
 - Click "View Request" on an output message to see the original input
 - Click "View Response" on an input message to see the transformed output

### Via Management API

Query messages programmatically:

**Get Messages for Integration**:
```http
GET /api/messages?integrationId={guid}&page=1&pageSize=50
```

**Query Parameters**:
- `integrationId` - Filter by integration (required)
- `direction` - Filter by Input/Output (optional)
- `status` - Filter by Success/Failed (optional)
- `startDate` - Messages after this date (optional)
- `endDate` - Messages before this date (optional)
- `correlationId` - Find linked messages (optional)
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 50, max: 100)

**Response**:
```json
{
 "items": [
 {
 "id": "msg_abc123",
 "integrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
 "integrationName": "Customer to ERP Integration",
 "direction": "Input",
 "payload": "{\"customer\": {\"firstName\": \"John\"}}",
 "isTruncated": false,
 "status": "Success",
 "errorMessage": null,
 "duration": null,
 "correlationId": "corr_xyz789",
 "timestamp": "2026-01-08T10:30:00Z",
 "metadata": {
 "sourceIp": "192.168.1.100",
 "userAgent": "MyApp/1.0"
 }
 }
 ],
 "totalCount": 150,
 "page": 1,
 "pageSize": 50
}
```

**Get Specific Message**:
```http
GET /api/messages/{messageId}
```

**Get Message Statistics**:
```http
GET /api/messages/statistics/{integrationId}?days=7
```

**Response**:
```json
{
 "integrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
 "totalMessages": 1250,
 "successMessages": 1180,
 "failedMessages": 70,
 "averageDurationMs": 245,
 "period": {
 "start": "2026-01-01T00:00:00Z",
 "end": "2026-01-08T23:59:59Z"
 }
}
```

## Configuration

### Per-Integration Settings

Enable or disable message capture for specific integrations:

```json
{
 "name": "Customer Integration",
 "enableMessageCapture": true,
 "messageCaptureConfig": {
 "captureInput": true,
 "captureOutput": true,
 "captureErrors": true,
 "maxPayloadSize": 1048576,
 "redactFields": ["password", "ssn", "creditCard"]
 }
}
```

**Settings**:
- `captureInput` - Capture incoming requests (default: true)
- `captureOutput` - Capture transformed responses (default: true)
- `captureErrors` - Always capture failed messages (default: true)
- `maxPayloadSize` - Max bytes to capture (default: 1MB)
- `redactFields` - Fields to redact from captured payloads

### Global Settings

Configure message capture globally:

**appsettings.json**:
```json
{
 "MessageCapture": {
 "Enabled": true,
 "Provider": "InMemory",
 "MaxPayloadSizeKB": 1024,
 "RetentionPeriod": "7.00:00:00",
 "MaxMessages": 10000,
 "CompressPayloads": false,
 "RedactSensitiveData": true,
 "SensitiveHeaders": ["Authorization", "X-API-Key", "Cookie"]
 }
}
```

### Disabling Message Capture

**Globally**:
```json
{
 "MessageCapture": {
 "Enabled": false
 }
}
```

**Per Integration**:
```json
{
 "enableMessageCapture": false
}
```

**Per Request** (via header):
```http
POST /api/map/customer-integration
X-Skip-Message-Capture: true
Content-Type: application/json

{...}
```

## Data Privacy and Security

### Redacting Sensitive Data

Automatically redact sensitive fields:

**Configuration**:
```csharp
builder.Services.AddInMemoryMessageCapture(options =>
{
 options.RedactFields = new[] { "password", "ssn", "creditCard", "apiKey" };
 options.RedactHeaders = new[] { "Authorization", "X-API-Key" };
});
```

**Before Redaction**:
```json
{
 "customer": {
 "name": "John Doe",
 "ssn": "123-45-6789",
 "password": "secret123"
 }
}
```

**After Redaction**:
```json
{
 "customer": {
 "name": "John Doe",
 "ssn": "***REDACTED***",
 "password": "***REDACTED***"
 }
}
```

### Payload Truncation

Large payloads are automatically truncated:

```csharp
public async Task CaptureAsync(CapturedMessage message)
{
 var maxSize = _options.MaxPayloadSizeKB * 1024;

 if (message.Payload.Length > maxSize)
 {
 message.Payload = message.Payload.Substring(0, maxSize);
 message.IsTruncated = true;
 message.Metadata["OriginalSize"] = originalSize.ToString();
 }

 // Store message
}
```

### Compliance Considerations

**GDPR / Data Privacy**:
- Set appropriate retention periods
- Implement data purging
- Redact PII (Personally Identifiable Information)
- Provide data export capabilities

**PCI DSS** (if handling payment data):
- Never capture full credit card numbers
- Redact CVV codes
- Encrypt captured messages
- Limit access to captured data

## Maintenance and Cleanup

### Automatic Purging

Configure automatic cleanup of old messages:

**In-Memory Provider**:
```csharp
// Runs every hour, removes messages older than retention period
builder.Services.AddInMemoryMessageCapture(options =>
{
 options.RetentionPeriod = TimeSpan.FromDays(7);
 options.CleanupInterval = TimeSpan.FromHours(1);
});
```

**Database Provider**:
```sql
-- Create scheduled job to purge old messages
DELETE FROM captured_messages
WHERE timestamp < NOW() - INTERVAL '30 days';
```

### Manual Purging

**Via API**:
```http
DELETE /api/messages/purge?integrationId={guid}&olderThan=2026-01-01
```

**Via SQL** (Database provider):
```sql
-- Purge all messages for an integration
DELETE FROM captured_messages
WHERE integration_id = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

-- Purge failed messages older than 30 days
DELETE FROM captured_messages
WHERE status = 'Failed'
 AND timestamp < NOW() - INTERVAL '30 days';
```

### Monitoring Storage Usage

**In-Memory Provider**:
```csharp
// Check memory usage
var stats = _messageCaptureProvider.GetStatistics();
Console.WriteLine($"Messages stored: {stats.MessageCount}");
Console.WriteLine($"Memory used: {stats.MemoryUsageBytes / 1024 / 1024} MB");
```

**Database Provider**:
```sql
-- Check table size
SELECT
 pg_size_pretty(pg_total_relation_size('captured_messages')) AS total_size,
 COUNT(*) AS message_count
FROM captured_messages;
```

## Troubleshooting

### Messages Not Being Captured

**Check 1**: Verify message capture is enabled
```json
{
 "MessageCapture": {
 "Enabled": true
 }
}
```

**Check 2**: Verify provider is registered
```csharp
// In Program.cs
builder.Services.AddInMemoryMessageCapture();
```

**Check 3**: Check integration setting
```json
{
 "enableMessageCapture": true
}
```

**Check 4**: Review behavior order
The MessageCaptureBehavior must be registered and have appropriate order (200).

### Truncated Payloads

**Issue**: Payloads are truncated (`IsTruncated = true`)

**Solution**: Increase max payload size
```json
{
 "MessageCapture": {
 "MaxPayloadSizeKB": 2048
 }
}
```

### High Memory Usage (In-Memory Provider)

**Issue**: Application using too much memory

**Solutions**:
1. Reduce retention period
2. Reduce max messages
3. Switch to database provider
4. Increase cleanup frequency

```json
{
 "MessageCapture": {
 "RetentionPeriod": "1.00:00:00",
 "MaxMessages": 1000,
 "CleanupInterval": "00:15:00"
 }
}
```

### Missing Correlation IDs

**Issue**: Can't link input and output messages

**Solution**: Ensure the same correlation ID is used for both captures. The MessageCaptureBehavior automatically handles this.

### Slow Queries (Database Provider)

**Issue**: Message queries are slow

**Solutions**:
1. Ensure indexes are created
2. Add composite indexes for common queries
3. Partition large tables
4. Archive old messages

```sql
-- Add composite index for common queries
CREATE INDEX idx_captured_messages_integration_timestamp
ON captured_messages(integration_id, timestamp DESC);

-- Partition by month (PostgreSQL)
CREATE TABLE captured_messages_2026_01
PARTITION OF captured_messages
FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
```

## Performance Considerations

### Async Capture

Message capture is always async to avoid blocking the request:

```csharp
public async Task<MappingResult> ExecuteAsync(
 MappingContext context,
 Func<MappingContext, Task<MappingResult>> next)
{
 var correlationId = Guid.NewGuid().ToString();

 // Capture input asynchronously
 _ = _provider.CaptureAsync(new CapturedMessage { ... });

 var result = await next(context);

 // Capture output asynchronously
 _ = _provider.CaptureAsync(new CapturedMessage { ... });

 return result;
}
```

### Batching (Database Provider)

Batch inserts for better performance:

```csharp
builder.Services.AddDatabaseMessageCapture(options =>
{
 options.BatchSize = 100; // Insert 100 messages at once
 options.BatchDelay = TimeSpan.FromSeconds(5); // Or after 5 seconds
});
```

### Sampling

Capture only a percentage of messages in high-traffic scenarios:

```csharp
builder.Services.AddMessageCapture(options =>
{
 options.SamplingRate = 0.1; // Capture 10% of messages
});
```

## Next Steps

- [Test Mode](test-mode.md) - Test integrations without hitting destinations
- [Behaviors](behaviors.md) - Understand the behavior pipeline
- [Architecture](architecture.md) - Learn how message capture fits in the system
