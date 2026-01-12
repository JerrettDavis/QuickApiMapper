# QuickApiMapper.Management.Api - Data Folder

## Overview

This folder contains data-related components for the QuickApiMapper Management API, including database seeders, sample data, and test utilities.

## Contents

### DemoDataSeeder.cs

**Purpose**: Automatically seeds the database with demo integration configurations on application startup.

**Key Features**:
- Implements `IHostedService` for startup execution
- Only runs in Development environment
- Configurable via `DemoModeOptions`
- Creates three pre-configured demo integrations
- Checks for existing data before seeding
- Supports force re-seeding

**Usage**:
```csharp
// Already registered in Program.cs
builder.Services.AddHostedService<DemoDataSeeder>();
```

**Configuration** (appsettings.Development.json):
```json
{
  "DemoMode": {
    "EnableDemoMode": true,
    "ForceReseed": false,
    "SampleMessageCount": 15,
    "FailedMessageCount": 3
  }
}
```

### SampleDemoRequests.http

**Purpose**: Collection of HTTP requests for testing demo integrations.

**Contents**:
- Management API integration queries
- Admin endpoints (migrate, seed)
- QuickApiMapper Web test requests (5 scenarios)
- SOAP to JSON status test
- Health checks
- Cleanup operations

**Usage**:
1. Open in Visual Studio, VS Code (with REST Client extension), or Rider
2. Click "Send Request" next to any request
3. View responses inline

**Test Scenarios**:
1. Standard order (STANDARD priority)
2. Express order (EXPRESS priority)
3. Overnight order (OVERNIGHT priority)
4. Multi-item order
5. International order

## Demo Integrations Created

### 1. Demo: JSON to SOAP Order Processing

Primary demonstration integration showing JSON-to-SOAP transformation.

- **Endpoint**: `/api/demo/fulfillment/submit`
- **Mappings**: 16 field mappings
- **Transformers**: ToLower, ToUpper, MapValue
- **SOAP Config**: Complete envelope with namespace

### 2. Demo: SOAP to JSON Fulfillment Status

Reverse integration demonstrating SOAP-to-JSON transformation.

- **Endpoint**: `/api/demo/fulfillment/status`
- **Mappings**: 5 field mappings
- **Purpose**: Status updates from SOAP warehouse system

### 3. Demo: RabbitMQ Order Batch Processing

Message queue integration for async processing.

- **Endpoint**: `/api/demo/batch/orders`
- **Mappings**: Same as Integration 1
- **Purpose**: Worker-based processing demonstration

## Development Workflow

### First Time Setup

1. **Start Management API**:
   ```bash
   cd src/QuickApiMapper.Management.Api
   dotnet run
   ```

2. **Verify seeding in logs**:
   ```
   [Management API] Demo mode enabled. Seeding demo data...
   [Management API] Creating demo integration: Demo: JSON to SOAP Order Processing
   [Management API] Successfully created: Demo: JSON to SOAP Order Processing
   ...
   [Management API] Demo data seeding completed successfully.
   ```

3. **Test with sample requests**:
   - Open `SampleDemoRequests.http`
   - Send test order request
   - Verify transformation

### Reset Demo Data

**Option 1: Force Reseed**
```json
{
  "DemoMode": {
    "EnableDemoMode": true,
    "ForceReseed": true  // Set to true
  }
}
```
Restart application, then set back to `false`.

**Option 2: Manual Deletion**
```bash
# Using Management API
DELETE https://localhost:7001/api/integrations/{demo-integration-id}
```

**Option 3: Database Reset**
```bash
# SQLite
rm quickapimapper.db
dotnet run  # Recreates database and seeds

# PostgreSQL
psql -c "DROP DATABASE quickapimapper; CREATE DATABASE quickapimapper;"
dotnet run
```

## Configuration Reference

### DemoModeOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableDemoMode` | bool | false | Enable/disable demo seeding |
| `ForceReseed` | bool | false | Force recreation of demo data |
| `SampleMessageCount` | int | 10 | Number of sample messages (future) |
| `FailedMessageCount` | int | 3 | Number of failed samples (future) |

### Environment-Specific Settings

**Production** (appsettings.json):
```json
{
  "DemoMode": {
    "EnableDemoMode": false  // Always disabled
  }
}
```

**Development** (appsettings.Development.json):
```json
{
  "DemoMode": {
    "EnableDemoMode": true  // Enabled by default
  }
}
```

## Testing Checklist

Before committing changes to demo data:

- [ ] Build succeeds without errors
- [ ] Demo data seeds successfully
- [ ] All three integrations created
- [ ] Field mappings are correct
- [ ] Transformers are applied
- [ ] SOAP configuration is valid
- [ ] Sample requests work
- [ ] Logs show no errors
- [ ] Can reset/reseed data
- [ ] Works with both SQLite and PostgreSQL

## Troubleshooting

### Demo data not seeding

**Check**:
1. Environment is Development: `ASPNETCORE_ENVIRONMENT=Development`
2. Configuration has `EnableDemoMode: true`
3. No existing demo integrations (unless `ForceReseed: true`)
4. Database connection is working
5. Check logs for detailed errors

**Common Issues**:
- **"Demo mode is disabled"**: Check environment and config
- **"Demo data already exists"**: Set `ForceReseed: true` or delete existing data
- **"Error seeding demo data"**: Check database connectivity and logs
- **Integration creation fails**: Verify entity models and relationships

### Sample requests not working

**Check**:
1. Management API is running (port 7001)
2. QuickApiMapper Web is running (port 5000)
3. Integrations were created successfully
4. Endpoints match configuration
5. Content-Type headers are correct

## Related Documentation

- **[DEMO_DATA.md](../../../docs/DEMO_DATA.md)** - Complete demo data documentation
- **[DEMO_QUICK_START.md](../../../DEMO_QUICK_START.md)** - Quick start guide
- **[DEMO_IMPLEMENTATION_PLAN.md](../../../docs/DEMO_IMPLEMENTATION_PLAN.md)** - Architecture details

## Future Enhancements

Planned additions to this folder:

- **SampleMessageSeeder.cs**: Seed message capture history
- **PerformanceDataSeeder.cs**: Generate metrics and statistics
- **ErrorScenarioSeeder.cs**: Create failed transformation samples
- **CustomTransformerSeeder.cs**: Seed demo custom transformers
- **TestDataGenerator.cs**: Generate varied test payloads

## Contributing

When adding new demo data or seeders:

1. Follow the existing `DemoDataSeeder` pattern
2. Implement `IHostedService` for startup execution
3. Use `DemoModeOptions` for configuration
4. Add sample HTTP requests to `SampleDemoRequests.http`
5. Update documentation
6. Add tests for seeding logic

## Support

For issues or questions:
1. Check [DEMO_DATA.md](../../../docs/DEMO_DATA.md) troubleshooting section
2. Review application logs
3. Verify configuration settings
4. Open an issue on GitHub

---

**Folder Structure**:
```
Data/
├── README.md                    # This file
├── DemoDataSeeder.cs            # Demo data seeder implementation
└── SampleDemoRequests.http      # HTTP test requests
```
