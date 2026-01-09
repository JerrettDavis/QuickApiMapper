# Persistence

QuickApiMapper uses a pluggable persistence layer to store integration configurations. This guide covers database setup, migrations, and custom providers.

## Overview

The persistence layer stores:
- **Integration Mappings** - Field mappings, transformers, static values
- **Protocol Configurations** - SOAP, gRPC, RabbitMQ, Service Bus settings
- **Toggle States** - Enable/disable input/output per integration

Supported databases:
- **SQLite** - Default for development
- **PostgreSQL** - Recommended for production

## Architecture

```
┌──────────────────────────────────────────┐
│ Persistence Abstractions │
│ ┌────────────────────────────────────┐ │
│ │ IIntegrationMappingRepository │ │
│ │ IIntegrationMappingProvider │ │
│ │ IUnitOfWork │ │
│ └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
 │
 ┌───────────┴───────────┐
 ▼ ▼
┌─────────────────┐ ┌──────────────────┐
│ SQLite Provider │ │ PostgreSQL │
│ │ │ Provider │
│ - Lightweight │ │ - Production- │
│ - File-based │ │ ready │
│ - Easy setup │ │ - ACID compliant │
│ - Dev/Testing │ │ - Scalable │
└─────────────────┘ └──────────────────┘
```

## SQLite (Development)

### Setup

SQLite is configured by default with no additional setup required.

**appsettings.json**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Data Source=quickapimapper.db"
 },
 "Persistence": {
 "Provider": "SQLite"
 }
}
```

### Database File Location

The database file is created in the application directory:
```
src/QuickApiMapper.Management.Api/quickapimapper.db
```

### Viewing Data

Use a SQLite browser:
- [DB Browser for SQLite](https://sqlitebrowser.org/)
- [SQLiteStudio](https://sqlitestudio.pl/)
- VS Code extension: SQLite

### Backup

Simply copy the database file:

```bash
cp quickapimapper.db quickapimapper.backup.db
```

### Limitations

- Not suitable for high concurrency
- Limited to single server
- File locking issues with multiple processes

## PostgreSQL (Production)

### Setup

#### 1. Install PostgreSQL

**Using Docker**:
```bash
docker run -d \
 --name quickapimapper-postgres \
 -e POSTGRES_USER=quickapimapper \
 -e POSTGRES_PASSWORD=your-secure-password \
 -e POSTGRES_DB=quickapimapper \
 -p 5432:5432 \
 postgres:17
```

**Or install directly**:
- [PostgreSQL Downloads](https://www.postgresql.org/download/)

#### 2. Configure Connection

**appsettings.Production.json**:
```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Host=localhost;Database=quickapimapper;Username=quickapimapper;Password=your-secure-password"
 },
 "Persistence": {
 "Provider": "PostgreSQL"
 }
}
```

#### 3. Run Migrations

Migrations are applied automatically on startup, or run manually:

```bash
cd src/QuickApiMapper.Persistence.PostgreSQL
dotnet ef database update --startup-project ../QuickApiMapper.Management.Api
```

### Connection Pooling

PostgreSQL uses connection pooling by default:

```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Host=localhost;Database=quickapimapper;Username=quickapimapper;Password=password;Minimum Pool Size=5;Maximum Pool Size=100"
 }
}
```

**Settings**:
- `Minimum Pool Size` - Minimum connections (default: 1)
- `Maximum Pool Size` - Maximum connections (default: 100)
- `Connection Lifetime` - Max connection age in seconds
- `Connection Idle Lifetime` - Idle timeout in seconds

### Backup

**Using pg_dump**:
```bash
pg_dump -h localhost -U quickapimapper quickapimapper > backup.sql
```

**Restore**:
```bash
psql -h localhost -U quickapimapper quickapimapper < backup.sql
```

**Automated Backups**:
```bash
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
pg_dump -h localhost -U quickapimapper quickapimapper | gzip > backup_$DATE.sql.gz

# Keep only last 7 days
find /backups -name "backup_*.sql.gz" -mtime +7 -delete
```

## Database Schema

### Core Tables

#### IntegrationMappings

Stores integration configurations:

```sql
CREATE TABLE integration_mappings (
 id UUID PRIMARY KEY,
 integration_id VARCHAR(200) UNIQUE NOT NULL,
 name VARCHAR(200) NOT NULL,
 description TEXT,
 source_type VARCHAR(50) NOT NULL,
 destination_type VARCHAR(50) NOT NULL,
 enable_input BOOLEAN NOT NULL DEFAULT TRUE,
 enable_output BOOLEAN NOT NULL DEFAULT TRUE,
 enable_message_capture BOOLEAN NOT NULL DEFAULT TRUE,
 created_at TIMESTAMP NOT NULL,
 updated_at TIMESTAMP NOT NULL
);

CREATE INDEX idx_integration_mappings_integration_id ON integration_mappings(integration_id);
```

#### FieldMappings

Stores field mapping definitions:

```sql
CREATE TABLE field_mappings (
 id UUID PRIMARY KEY,
 integration_mapping_id UUID NOT NULL,
 source_path VARCHAR(500) NOT NULL,
 destination_path VARCHAR(500) NOT NULL,
 FOREIGN KEY (integration_mapping_id) REFERENCES integration_mappings(id) ON DELETE CASCADE
);

CREATE INDEX idx_field_mappings_integration ON field_mappings(integration_mapping_id);
```

#### TransformerConfigs

Stores transformer configurations:

```sql
CREATE TABLE transformer_configs (
 id UUID PRIMARY KEY,
 field_mapping_id UUID NOT NULL,
 transformer_name VARCHAR(200) NOT NULL,
 order_index INT NOT NULL,
 parameters JSONB,
 FOREIGN KEY (field_mapping_id) REFERENCES field_mappings(id) ON DELETE CASCADE
);

CREATE INDEX idx_transformer_configs_field_mapping ON transformer_configs(field_mapping_id);
```

#### StaticValues

Stores static value mappings:

```sql
CREATE TABLE static_values (
 id UUID PRIMARY KEY,
 integration_mapping_id UUID NOT NULL,
 destination_path VARCHAR(500) NOT NULL,
 static_value TEXT NOT NULL,
 FOREIGN KEY (integration_mapping_id) REFERENCES integration_mappings(id) ON DELETE CASCADE
);

CREATE INDEX idx_static_values_integration ON static_values(integration_mapping_id);
```

### Protocol-Specific Tables

#### SoapConfigs

```sql
CREATE TABLE soap_configs (
 id UUID PRIMARY KEY,
 integration_mapping_id UUID UNIQUE NOT NULL,
 endpoint_url VARCHAR(500) NOT NULL,
 soap_action VARCHAR(500),
 method_name VARCHAR(200) NOT NULL,
 target_namespace VARCHAR(500),
 soap_version VARCHAR(10),
 FOREIGN KEY (integration_mapping_id) REFERENCES integration_mappings(id) ON DELETE CASCADE
);
```

#### GrpcConfigs

```sql
CREATE TABLE grpc_configs (
 id UUID PRIMARY KEY,
 integration_mapping_id UUID UNIQUE NOT NULL,
 endpoint_url VARCHAR(500) NOT NULL,
 service_name VARCHAR(200) NOT NULL,
 method_name VARCHAR(200) NOT NULL,
 proto_file_path VARCHAR(500),
 FOREIGN KEY (integration_mapping_id) REFERENCES integration_mappings(id) ON DELETE CASCADE
);
```

## Migrations

### Creating Migrations

**Using EF Core CLI**:

```bash
# For PostgreSQL
cd src/QuickApiMapper.Persistence.PostgreSQL
dotnet ef migrations add MigrationName --startup-project ../QuickApiMapper.Management.Api

# For SQLite
cd src/QuickApiMapper.Persistence.SQLite
dotnet ef migrations add MigrationName --startup-project ../QuickApiMapper.Management.Api
```

### Applying Migrations

**Automatic** (on application startup):
```csharp
await context.Database.MigrateAsync();
```

**Manual** (using CLI):
```bash
dotnet ef database update --startup-project ../QuickApiMapper.Management.Api
```

**Specific Migration**:
```bash
dotnet ef database update TargetMigrationName --startup-project ../QuickApiMapper.Management.Api
```

### Rollback

```bash
dotnet ef database update PreviousMigrationName --startup-project ../QuickApiMapper.Management.Api
```

### Migration History

View applied migrations:

**PostgreSQL**:
```sql
SELECT * FROM __EFMigrationsHistory ORDER BY migration_id;
```

**SQLite**:
```sql
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

## Seeding Data

### Automatic Seeding

Sample integrations are seeded on first run:

**Migration**: `SeedSampleIntegrations`

Includes 6 example integrations:
- Customer to ERP
- Vendor to ERP
- Order to Fulfillment
- Employee Sync
- Product Catalog
- Invoice Processing

### Custom Seeding

Add seed data in migrations:

```csharp
public partial class SeedCustomData : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
 migrationBuilder.InsertData(
 table: "IntegrationMappings",
 columns: new[] { "Id", "IntegrationId", "Name", "SourceType", "DestinationType" },
 values: new object[] {
 Guid.NewGuid(),
 "my-integration",
 "My Custom Integration",
 "JSON",
 "SOAP"
 });
 }
}
```

## Custom Persistence Providers

### Implementing Custom Provider

Create a custom provider for other databases (MySQL, SQL Server, MongoDB, etc.):

**1. Create DbContext**:

```csharp
public class CustomDbContext : DbContext
{
 public DbSet<IntegrationMappingEntity> IntegrationMappings { get; set; }
 public DbSet<FieldMappingEntity> FieldMappings { get; set; }

 public CustomDbContext(DbContextOptions<CustomDbContext> options)
 : base(options)
 {
 }

 protected override void OnModelCreating(ModelBuilder modelBuilder)
 {
 // Configure entity mappings
 }
}
```

**2. Implement Repository**:

```csharp
public class CustomRepository : IIntegrationMappingRepository
{
 private readonly CustomDbContext _context;

 public CustomRepository(CustomDbContext context)
 {
 _context = context;
 }

 public async Task<IntegrationMappingEntity?> GetByIntegrationIdAsync(string integrationId)
 {
 return await _context.IntegrationMappings
 .Include(x => x.FieldMappings)
 .FirstOrDefaultAsync(x => x.IntegrationId == integrationId);
 }

 // Implement other methods...
}
```

**3. Register in DI**:

```csharp
builder.Services.AddDbContext<CustomDbContext>(options =>
 options.UseMyDatabase(connectionString));

builder.Services.AddScoped<IIntegrationMappingRepository, CustomRepository>();
builder.Services.AddScoped<IUnitOfWork, CustomUnitOfWork>();
```

## Performance Optimization

### Indexing

Ensure proper indexes exist:

```sql
-- Integration lookups
CREATE INDEX idx_integration_mappings_integration_id ON integration_mappings(integration_id);

-- Field mapping queries
CREATE INDEX idx_field_mappings_integration ON field_mappings(integration_mapping_id);

-- Transformer queries
CREATE INDEX idx_transformer_configs_field_mapping ON transformer_configs(field_mapping_id);
```

### Query Optimization

Use eager loading to avoid N+1 queries:

```csharp
var integration = await _context.IntegrationMappings
 .Include(x => x.FieldMappings)
 .ThenInclude(x => x.Transformers)
 .Include(x => x.StaticValues)
 .Include(x => x.SoapConfig)
 .FirstOrDefaultAsync(x => x.IntegrationId == integrationId);
```

### Caching

Use configuration caching:

```csharp
builder.Services.AddCachedConfigurationProvider(options =>
{
 options.CacheDuration = TimeSpan.FromMinutes(5);
 options.MaxCacheSize = 1000;
});
```

## Monitoring

### Connection Pool Monitoring

**PostgreSQL**:
```sql
SELECT *
FROM pg_stat_activity
WHERE datname = 'quickapimapper';
```

### Table Sizes

**PostgreSQL**:
```sql
SELECT
 schemaname,
 tablename,
 pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Slow Queries

Enable query logging:

```json
{
 "Logging": {
 "LogLevel": {
 "Microsoft.EntityFrameworkCore.Database.Command": "Information"
 }
 }
}
```

## Troubleshooting

### Migration Lock

**Issue**: `__EFMigrationsLock` prevents migrations

**Solution**:
```sql
DELETE FROM __EFMigrationsLock;
```

### Connection Pool Exhausted

**Error**: `Connection pool exhausted`

**Solutions**:
1. Increase pool size
2. Dispose DbContext properly
3. Check for connection leaks

### Slow Queries

**Solutions**:
1. Add missing indexes
2. Use eager loading
3. Implement caching
4. Optimize query patterns

## Next Steps

- [Configuration](configuration.md) - Application configuration
- [Deployment](deployment.md) - Production deployment
- [Architecture](architecture.md) - System architecture
