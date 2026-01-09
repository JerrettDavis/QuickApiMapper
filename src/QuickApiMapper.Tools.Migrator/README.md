# QuickApiMapper Configuration Migrator

A command-line tool for migrating integration configurations from `appsettings.json` to database storage.

## Purpose

This tool helps you transition from file-based configuration (appsettings.json) to database-backed persistence without manual data entry. It reads your existing integration mappings and writes them to either PostgreSQL or SQLite databases.

## Prerequisites

- .NET 10.0 SDK or later
- PostgreSQL server (if using PostgreSQL backend) or SQLite
- Access credentials to your target database

## Installation

Build the tool from source:

```bash
cd QuickApiMapper.Tools.Migrator
dotnet build
```

Or publish as a standalone executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Usage

### Basic Syntax

```bash
QuickApiMapper.Tools.Migrator --source <file> --db-type <type> --connection <connection-string> [--overwrite]
```

### Options

| Option | Short | Description | Required |
|--------|-------|-------------|----------|
| `--source` | `-s` | Path to appsettings.json file | Yes |
| `--db-type` | `-t` | Database type: `postgresql` or `sqlite` | Yes |
| `--connection` | `-c` | Database connection string | Yes |
| `--overwrite` | `-o` | Overwrite existing integrations | No |
| `--help` | `-h` | Show help message | No |

### Examples

#### Migrate to PostgreSQL

```bash
dotnet run --project QuickApiMapper.Tools.Migrator -- \
 --source "C:\MyApp\appsettings.json" \
 --db-type postgresql \
 --connection "Host=localhost;Database=quickapimapper;Username=postgres;Password=yourpassword"
```

#### Migrate to SQLite

```bash
dotnet run --project QuickApiMapper.Tools.Migrator -- \
 --source "C:\MyApp\appsettings.json" \
 --db-type sqlite \
 --connection "Data Source=quickapimapper.db"
```

#### Overwrite Existing Integrations

```bash
dotnet run --project QuickApiMapper.Tools.Migrator -- \
 --source appsettings.json \
 --db-type postgresql \
 --connection "Host=localhost;Database=quickapimapper;Username=postgres;Password=yourpassword" \
 --overwrite
```

## What Gets Migrated

The tool migrates all integration configurations from the `ApiMapping` section of your appsettings.json:

- **Integration Metadata**: Name, endpoint, source/destination types, URLs
- **Field Mappings**: Source → destination mappings with transformation pipelines
- **Transformers**: Transformer configurations with arguments
- **Static Values**: Both integration-specific and global static values
- **SOAP Configuration**: Header fields, body fields, namespaces, and wrappers

## Database Schema

The tool automatically creates the necessary database schema on first run:

- `integration_mappings` - Core integration configurations
- `field_mappings` - Field-level mapping definitions
- `transformers` - Transformer configurations per field
- `static_values` - Static value storage (global and per-integration)
- `soap_configurations` - SOAP-specific configuration
- `soap_fields` - SOAP header and body field definitions

## Migration Process

1. **Read Configuration**: Parses the `ApiMapping` section from appsettings.json
2. **Connect to Database**: Establishes connection and applies migrations
3. **Check for Duplicates**: Verifies if integrations already exist (by name)
4. **Transform Data**: Converts contract models to entity models
5. **Write to Database**: Inserts or updates records with proper relationships
6. **Log Results**: Reports success/failure for each integration

## Error Handling

- **File Not Found**: Verifies source file exists before proceeding
- **Invalid Database Type**: Only accepts `postgresql` or `sqlite`
- **Duplicate Integrations**: Skips duplicates unless `--overwrite` is specified
- **Database Errors**: Logs detailed error messages for troubleshooting
- **Malformed JSON**: Reports parsing errors with helpful context

## Post-Migration Steps

After successful migration:

1. **Verify Data**: Check the database to ensure all integrations were migrated
 ```sql
 SELECT * FROM integration_mappings;
 ```

2. **Update QuickApiMapper.Web**: Configure to use database provider
 ```csharp
 builder.Services.AddQuickApiMapperWithPersistence(options =>
 {
 options.UseDatabase = true;
 options.ConnectionString = builder.Configuration.GetConnectionString("QuickApiMapper");
 options.EnableCaching = true;
 });
 ```

3. **Test Integrations**: Verify each integration works correctly with database-backed configuration

4. **Backup appsettings.json**: Keep original file as backup
 ```bash
 copy appsettings.json appsettings.json.backup
 ```

## Rollback

If you need to rollback to file-based configuration:

1. Set `UseDatabase = false` in your application configuration
2. Restore the original appsettings.json from backup
3. Restart the application

## Troubleshooting

### Connection Refused

**Error**: Could not connect to the database

**Solution**: Verify database is running and connection string is correct

```bash
# PostgreSQL
psql -h localhost -U postgres -d quickapimapper

# SQLite
sqlite3 quickapimapper.db
```

### Duplicate Integrations

**Error**: Integration already exists

**Solution**: Use `--overwrite` flag to replace existing integrations

### Schema Not Created

**Error**: Table does not exist

**Solution**: Ensure EF Core migrations are applied automatically. The tool does this by default, but you can manually apply migrations:

```bash
dotnet ef database update --project QuickApiMapper.Persistence.PostgreSQL
```

## Development

To modify or extend the migrator:

1. **Add New Entity Types**: Update `MapToEntity()` method
2. **Support New Protocols**: Add mapping logic for gRPC, ServiceBus, RabbitMQ configurations
3. **Custom Validation**: Add validation logic in `ParseArguments()`
4. **Logging**: Modify logging levels in `CreateHost()`

## See Also

- [Persistence Architecture](../docs/architecture/persistence-layer.md)
- [Provider Pattern Guide](../QuickApiMapper.Application/Providers/README.md)
- [Database Configuration](../docs/guides/migration.md)
