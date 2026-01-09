# Configuration

This guide covers all configuration options for QuickApiMapper applications.

## Configuration Files

QuickApiMapper uses standard .NET configuration:

- **appsettings.json** - Default settings
- **appsettings.Development.json** - Development overrides
- **appsettings.Production.json** - Production overrides
- **Environment Variables** - Runtime configuration
- **Command Line Arguments** - Startup configuration

## Application Configuration

### Management API

**appsettings.json**:
```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information",
 "Microsoft.AspNetCore": "Warning",
 "Microsoft.EntityFrameworkCore": "Warning"
 }
 },
 "AllowedHosts": "*",
 "ConnectionStrings": {
 "QuickApiMapper": "Data Source=quickapimapper.db"
 },
 "Persistence": {
 "Provider": "SQLite"
 },
 "Cors": {
 "AllowedOrigins": ["http://localhost:5173"]
 }
}
```

### Runtime Web API

**appsettings.json**:
```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information",
 "Microsoft.AspNetCore": "Warning"
 }
 },
 "ConnectionStrings": {
 "QuickApiMapper": "Data Source=quickapimapper.db"
 },
 "Persistence": {
 "Provider": "SQLite",
 "CacheEnabled": true,
 "CacheDuration": "00:05:00"
 },
 "MessageCapture": {
 "Enabled": true,
 "Provider": "InMemory",
 "MaxPayloadSizeKB": 1024,
 "RetentionPeriod": "7.00:00:00"
 }
}
```

### Web Designer

**appsettings.json**:
```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information"
 }
 },
 "ApiClient": {
 "BaseUrl": "http://localhost:5074",
 "Timeout": "00:00:30"
 }
}
```

## Database Configuration

### SQLite

**Connection String**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Data Source=quickapimapper.db"
 }
}
```

**Options**:
- `Data Source` - Database file path
- `Mode` - ReadWrite, ReadOnly, Memory
- `Cache` - Shared, Private
- `Foreign Keys` - True, False

**Example**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Data Source=quickapimapper.db;Mode=ReadWrite;Foreign Keys=True"
 }
}
```

### PostgreSQL

**Connection String**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Host=localhost;Database=quickapimapper;Username=user;Password=pass"
 }
}
```

**Options**:
- `Host` - Server address
- `Port` - Port number (default: 5432)
- `Database` - Database name
- `Username` - User name
- `Password` - Password
- `SSL Mode` - Disable, Allow, Prefer, Require
- `Pooling` - True, False
- `Minimum Pool Size` - Min connections
- `Maximum Pool Size` - Max connections
- `Timeout` - Connection timeout
- `Command Timeout` - Query timeout

**Example**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Host=postgres.example.com;Port=5432;Database=quickapimapper;Username=quickapi;Password=securepass;SSL Mode=Require;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100"
 }
}
```

## Persistence Configuration

```json
{
 "Persistence": {
 "Provider": "PostgreSQL",
 "CacheEnabled": true,
 "CacheDuration": "00:05:00",
 "MaxCacheSize": 1000,
 "EnableQueryLogging": false
 }
}
```

**Options**:
- `Provider` - SQLite or PostgreSQL
- `CacheEnabled` - Enable configuration caching
- `CacheDuration` - Cache TTL (TimeSpan format)
- `MaxCacheSize` - Max cached integrations
- `EnableQueryLogging` - Log EF Core queries

## Message Capture Configuration

```json
{
 "MessageCapture": {
 "Enabled": true,
 "Provider": "InMemory",
 "MaxPayloadSizeKB": 1024,
 "RetentionPeriod": "7.00:00:00",
 "MaxMessages": 10000,
 "CleanupInterval": "01:00:00",
 "CompressPayloads": false,
 "RedactSensitiveData": true,
 "SensitiveFields": ["password", "ssn", "creditCard"],
 "SensitiveHeaders": ["Authorization", "X-API-Key"]
 }
}
```

**Options**:
- `Enabled` - Enable/disable message capture
- `Provider` - InMemory or Database
- `MaxPayloadSizeKB` - Max payload size (KB)
- `RetentionPeriod` - Keep messages for (TimeSpan)
- `MaxMessages` - Max messages in memory
- `CleanupInterval` - Cleanup frequency
- `CompressPayloads` - Compress large payloads
- `RedactSensitiveData` - Enable redaction
- `SensitiveFields` - Fields to redact
- `SensitiveHeaders` - Headers to redact

## HTTP Client Configuration

```json
{
 "HttpClient": {
 "DefaultTimeout": "00:00:30",
 "MaxConnectionsPerServer": 100,
 "PooledConnectionLifetime": "00:05:00",
 "PooledConnectionIdleTimeout": "00:02:00"
 }
}
```

**Options**:
- `DefaultTimeout` - Default request timeout
- `MaxConnectionsPerServer` - Max connections per host
- `PooledConnectionLifetime` - Max connection age
- `PooledConnectionIdleTimeout` - Idle timeout

## CORS Configuration

```json
{
 "Cors": {
 "AllowedOrigins": [
 "http://localhost:5173",
 "https://designer.example.com"
 ],
 "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
 "AllowedHeaders": ["Content-Type", "Authorization"],
 "AllowCredentials": true,
 "MaxAge": 3600
 }
}
```

## Logging Configuration

### Console Logging

```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information",
 "Microsoft": "Warning",
 "Microsoft.Hosting.Lifetime": "Information",
 "Microsoft.AspNetCore": "Warning",
 "Microsoft.EntityFrameworkCore": "Warning"
 }
 }
}
```

### File Logging (Serilog)

**appsettings.json**:
```json
{
 "Serilog": {
 "MinimumLevel": {
 "Default": "Information",
 "Override": {
 "Microsoft": "Warning",
 "Microsoft.AspNetCore": "Warning"
 }
 },
 "WriteTo": [
 {
 "Name": "Console"
 },
 {
 "Name": "File",
 "Args": {
 "path": "logs/quickapimapper-.log",
 "rollingInterval": "Day",
 "retainedFileCountLimit": 7,
 "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
 }
 }
 ]
 }
}
```

### Application Insights

```json
{
 "ApplicationInsights": {
 "InstrumentationKey": "your-key-here",
 "EnableAdaptiveSampling": true,
 "EnableQuickPulseMetricStream": true
 }
}
```

## Health Checks

```json
{
 "HealthChecks": {
 "Database": {
 "Enabled": true,
 "Timeout": "00:00:05"
 },
 "RabbitMQ": {
 "Enabled": true,
 "ConnectionString": "amqp://localhost"
 },
 "ServiceBus": {
 "Enabled": true,
 "ConnectionString": "Endpoint=sb://..."
 }
 }
}
```

## Security Configuration

### Authentication

```json
{
 "Authentication": {
 "Enabled": false,
 "Authority": "https://auth.example.com",
 "Audience": "quickapimapper-api",
 "RequireHttpsMetadata": true
 }
}
```

### API Keys

```json
{
 "ApiKeys": {
 "Enabled": false,
 "HeaderName": "X-API-Key",
 "Keys": [
 {
 "Key": "key1",
 "Name": "Client 1",
 "Permissions": ["read", "write"]
 }
 ]
 }
}
```

## Environment Variables

Override configuration with environment variables:

### Naming Convention

```
AppName__Section__Property
```

**Examples**:
```bash
# Connection string
export ConnectionStrings__QuickApiMapper="Host=postgres;Database=quickapimapper;..."

# Persistence provider
export Persistence__Provider="PostgreSQL"

# Message capture
export MessageCapture__Enabled="true"
export MessageCapture__MaxPayloadSizeKB="2048"
```

### Docker

**docker-compose.yml**:
```yaml
services:
 web-api:
 environment:
 - ConnectionStrings__QuickApiMapper=Host=postgres;Database=quickapimapper;...
 - Persistence__Provider=PostgreSQL
 - MessageCapture__Enabled=true
```

### Kubernetes

**deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
 name: quickapimapper-web
spec:
 template:
 spec:
 containers:
 - name: web
 env:
 - name: ConnectionStrings__QuickApiMapper
 valueFrom:
 secretKeyRef:
 name: db-connection
 key: connection-string
 - name: Persistence__Provider
 value: "PostgreSQL"
```

## Secrets Management

### User Secrets (Development)

```bash
# Initialize
dotnet user-secrets init --project src/QuickApiMapper.Management.Api

# Set secret
dotnet user-secrets set "ConnectionStrings:QuickApiMapper" "Host=localhost;..." --project src/QuickApiMapper.Management.Api
```

### Azure Key Vault

**Program.cs**:
```csharp
builder.Configuration.AddAzureKeyVault(
 new Uri("https://your-keyvault.vault.azure.net/"),
 new DefaultAzureCredential());
```

**Key Vault Secrets**:
- `ConnectionStrings--QuickApiMapper`
- `MessageCapture--MaxPayloadSizeKB`
- `ApiKeys--Keys--0--Key`

## Configuration Validation

### Startup Validation

```csharp
builder.Services.AddOptions<PersistenceOptions>()
 .Bind(builder.Configuration.GetSection("Persistence"))
 .ValidateDataAnnotations()
 .ValidateOnStart();
```

### Options Pattern

```csharp
public class PersistenceOptions
{
 [Required]
 public string Provider { get; set; }

 [Range(1, 3600)]
 public int CacheDurationSeconds { get; set; } = 300;
}
```

## Next Steps

- [Deployment](deployment.md) - Deploy to production
- [Persistence](persistence.md) - Database configuration
- [Architecture](architecture.md) - System architecture
