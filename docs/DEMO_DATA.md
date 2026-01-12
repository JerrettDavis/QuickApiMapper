# QuickApiMapper Demo Data Documentation

## Overview

The QuickApiMapper demo data seeder automatically populates your development database with pre-configured integrations that showcase the system's capabilities. This allows you to immediately test and demonstrate JSON-to-SOAP transformations, field mappings, and message capture without manual configuration.

## What Data is Seeded

The demo seeder creates **three pre-configured integrations** that demonstrate real-world e-commerce order fulfillment scenarios:

### 1. Demo: JSON to SOAP Order Processing

**Primary demonstration integration** - Shows complete JSON to SOAP transformation.

- **Endpoint**: `/api/demo/fulfillment/submit`
- **Source Type**: JSON
- **Destination Type**: SOAP
- **Destination URL**: `http://demo-soapapi/WarehouseService.asmx`
- **Status**: Active, with message capture enabled

**Field Mappings** (16 mappings total):

| Source (JSONPath) | Destination (XPath) | Transformers |
|-------------------|---------------------|--------------|
| `$.orderId` | `/OrderNumber` | - |
| `$.customerName` | `/CustomerInfo/Name` | - |
| `$.customerEmail` | `/CustomerInfo/ContactEmail` | `ToLower` |
| `$.orderDate` | `/OrderDateTime` | - |
| `$.totalAmount` | `/TotalValue` | - |
| `$.currency` | `/CurrencyCode` | - |
| `$.items[*].sku` | `/LineItems/Item/SKU` | `ToUpper` |
| `$.items[*].productName` | `/LineItems/Item/Description` | - |
| `$.items[*].quantity` | `/LineItems/Item/Qty` | - |
| `$.items[*].unitPrice` | `/LineItems/Item/Price` | - |
| `$.shippingAddress.street` | `/DeliveryAddress/AddressLine1` | - |
| `$.shippingAddress.city` | `/DeliveryAddress/City` | - |
| `$.shippingAddress.state` | `/DeliveryAddress/StateProvince` | - |
| `$.shippingAddress.postalCode` | `/DeliveryAddress/PostalCode` | - |
| `$.shippingAddress.country` | `/DeliveryAddress/CountryCode` | - |
| `$.priority` | `/PriorityCode` | `MapValue` (STANDARD→STD, EXPRESS→EXP, OVERNIGHT→OVN) |

**SOAP Configuration**:
- Action: `http://warehouse.example.com/SubmitFulfillmentRequest`
- Namespace: `http://warehouse.example.com/`
- Root Element: `SubmitFulfillmentRequest`

### 2. Demo: SOAP to JSON Fulfillment Status

**Reverse integration** - Demonstrates SOAP to JSON transformation.

- **Endpoint**: `/api/demo/fulfillment/status`
- **Source Type**: SOAP
- **Destination Type**: JSON
- **Destination URL**: `http://demo-jsonapi/api/orders/status`
- **Status**: Active

**Field Mappings** (5 mappings):
- Order number, status, tracking number, estimated delivery, and last updated timestamp

### 3. Demo: RabbitMQ Order Batch Processing

**Message queue integration** - Shows async processing via RabbitMQ.

- **Endpoint**: `/api/demo/batch/orders`
- **Source Type**: JSON
- **Destination Type**: SOAP
- **Destination URL**: `http://demo-soapapi/WarehouseService.asmx`
- **Status**: Active
- **Purpose**: Demonstrates worker-based message processing

Same field mappings as Integration #1 for consistency.

## How to Enable/Disable Demo Mode

### Enable Demo Mode

**Option 1: Configuration File** (Recommended for development)

Edit `appsettings.Development.json`:

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

**Option 2: Environment Variable**

```bash
# Windows
set DemoMode__EnableDemoMode=true

# Linux/macOS
export DemoMode__EnableDemoMode=true
```

**Option 3: Aspire AppHost**

```csharp
var managementApi = builder.AddProject<Projects.QuickApiMapper_Management_Api>("management-api")
    .WithEnvironment("DemoMode__EnableDemoMode", "true")
    .WithEnvironment("DemoMode__ForceReseed", "false");
```

### Disable Demo Mode

Set `EnableDemoMode` to `false` in `appsettings.json`:

```json
{
  "DemoMode": {
    "EnableDemoMode": false
  }
}
```

**Important**: Demo mode only runs in the **Development** environment. It is automatically disabled in Production, Staging, or any non-Development environment for security.

## Configuration Options

### DemoModeOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableDemoMode` | `bool` | `false` | Master switch to enable/disable demo data seeding |
| `ForceReseed` | `bool` | `false` | If true, deletes existing demo data and re-seeds on startup |
| `SampleMessageCount` | `int` | `10` | Number of sample captured messages to generate (future enhancement) |
| `FailedMessageCount` | `int` | `3` | Number of failed message samples for error demonstration (future enhancement) |

### When Demo Data is Seeded

Demo data seeding occurs **only** when:

1. ✅ Application is running in **Development** environment
2. ✅ `DemoMode.EnableDemoMode` is set to `true`
3. ✅ No existing integrations with names starting with "Demo:" exist **OR** `ForceReseed` is `true`

### Seeding Behavior

- **First Run**: Creates all demo integrations
- **Subsequent Runs**: Skips seeding if demo data already exists
- **Force Reseed**: Set `ForceReseed: true` to delete and recreate demo integrations on every startup

## How to Reset Demo Data

### Method 1: Force Reseed (Automatic)

Set `ForceReseed` to `true` in configuration and restart the application:

```json
{
  "DemoMode": {
    "EnableDemoMode": true,
    "ForceReseed": true
  }
}
```

After restart, set `ForceReseed` back to `false` to prevent continuous re-seeding.

### Method 2: Manual API Call

Use the Management API to delete demo integrations:

```bash
# Get all integrations
GET https://localhost:7001/api/integrations

# Delete each demo integration by ID
DELETE https://localhost:7001/api/integrations/{id}
```

### Method 3: Database Reset

**SQLite** (default):
```bash
# Stop the application
# Delete the database file
rm quickapimapper.db

# Restart - migrations will recreate the database and demo data will seed
```

**PostgreSQL**:
```sql
-- Connect to PostgreSQL
DROP DATABASE quickapimapper;
CREATE DATABASE quickapimapper;

-- Restart application - migrations will run and demo data will seed
```

### Method 4: Admin Seed Endpoint (Development Only)

The Management API includes a development-only endpoint to trigger manual seeding:

```bash
POST https://localhost:7001/api/admin/seed
```

Response:
```json
{
  "success": true,
  "message": "Seeded 3 sample integrations",
  "seeded": true
}
```

If data already exists:
```json
{
  "success": true,
  "message": "Database already has 3 integration(s)",
  "seeded": false
}
```

## Sample Test Requests

### Test JSON to SOAP Order Processing

**Request**:
```bash
POST http://localhost:5000/api/demo/fulfillment/submit
Content-Type: application/json

{
  "orderId": "ORD-2026-001",
  "customerName": "John Smith",
  "customerEmail": "JOHN.SMITH@EXAMPLE.COM",
  "orderDate": "2026-01-10T14:30:00Z",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [
    {
      "sku": "laptop-xps15",
      "productName": "Dell XPS 15 Laptop",
      "quantity": 1,
      "unitPrice": 599.99
    }
  ],
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Seattle",
    "state": "WA",
    "postalCode": "98101",
    "country": "USA"
  },
  "priority": "STANDARD"
}
```

**Expected SOAP Output**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <SubmitFulfillmentRequest xmlns="http://warehouse.example.com/">
      <OrderNumber>ORD-2026-001</OrderNumber>
      <CustomerInfo>
        <Name>John Smith</Name>
        <ContactEmail>john.smith@example.com</ContactEmail>
      </CustomerInfo>
      <OrderDateTime>2026-01-10T14:30:00Z</OrderDateTime>
      <TotalValue>599.99</TotalValue>
      <CurrencyCode>USD</CurrencyCode>
      <LineItems>
        <Item>
          <SKU>LAPTOP-XPS15</SKU>
          <Description>Dell XPS 15 Laptop</Description>
          <Qty>1</Qty>
          <Price>599.99</Price>
        </Item>
      </LineItems>
      <DeliveryAddress>
        <AddressLine1>123 Main St</AddressLine1>
        <City>Seattle</City>
        <StateProvince>WA</StateProvince>
        <PostalCode>98101</PostalCode>
        <CountryCode>USA</CountryCode>
      </DeliveryAddress>
      <PriorityCode>STD</PriorityCode>
    </SubmitFulfillmentRequest>
  </soap:Body>
</soap:Envelope>
```

**Key Transformations to Observe**:
- Email converted to lowercase: `JOHN.SMITH@EXAMPLE.COM` → `john.smith@example.com`
- SKU converted to uppercase: `laptop-xps15` → `LAPTOP-XPS15`
- Priority mapped: `STANDARD` → `STD`

### Test with Different Priority Levels

**Express Shipping**:
```json
{
  "orderId": "ORD-2026-002",
  "priority": "EXPRESS"
  // ... other fields
}
```
Results in `<PriorityCode>EXP</PriorityCode>`

**Overnight Shipping**:
```json
{
  "orderId": "ORD-2026-003",
  "priority": "OVERNIGHT"
  // ... other fields
}
```
Results in `<PriorityCode>OVN</PriorityCode>`

### Test SOAP to JSON Status

**Request** (SOAP):
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <FulfillmentStatusResponse>
      <OrderNumber>ORD-2026-001</OrderNumber>
      <Status>SHIPPED</Status>
      <TrackingNumber>1Z999AA10123456784</TrackingNumber>
      <EstimatedDelivery>2026-01-15T00:00:00Z</EstimatedDelivery>
      <LastUpdated>2026-01-10T16:45:00Z</LastUpdated>
    </FulfillmentStatusResponse>
  </soap:Body>
</soap:Envelope>
```

**Expected JSON Output**:
```json
{
  "orderId": "ORD-2026-001",
  "status": "shipped",
  "trackingNumber": "1Z999AA10123456784",
  "estimatedDeliveryDate": "2026-01-15T00:00:00Z",
  "lastUpdated": "2026-01-10T16:45:00Z"
}
```

## Viewing Demo Data

### Via Management API

**List all integrations**:
```bash
GET https://localhost:7001/api/integrations
```

**Get specific demo integration**:
```bash
GET https://localhost:7001/api/integrations?name=Demo:%20JSON%20to%20SOAP%20Order%20Processing
```

### Via Designer Dashboard

1. Navigate to: `https://localhost:7002` (Designer Web UI)
2. Click on "Integrations" in the navigation menu
3. You'll see all three demo integrations listed with "Demo:" prefix
4. Click on any integration to view its complete configuration:
   - Field mappings
   - Transformers
   - SOAP configuration
   - Static values

### Via Database

**SQLite**:
```bash
sqlite3 quickapimapper.db
```

```sql
-- View demo integrations
SELECT Id, Name, Endpoint, SourceType, DestinationType, IsActive
FROM integrationmappings
WHERE Name LIKE 'Demo:%';

-- View field mappings for a demo integration
SELECT fm.Source, fm.Destination, fm.Order
FROM fieldmappings fm
JOIN integrationmappings im ON fm.IntegrationMappingId = im.Id
WHERE im.Name = 'Demo: JSON to SOAP Order Processing'
ORDER BY fm.Order;

-- View transformers
SELECT t.Name, t.Order, t.Arguments
FROM transformers t
JOIN fieldmappings fm ON t.FieldMappingId = fm.Id
JOIN integrationmappings im ON fm.IntegrationMappingId = im.Id
WHERE im.Name = 'Demo: JSON to SOAP Order Processing'
ORDER BY t.Order;
```

**PostgreSQL**:
```sql
-- Same queries as SQLite, but connect via psql:
psql -h localhost -U postgres -d quickapimapper
```

## Troubleshooting

### Demo Data Not Seeding

**Check 1: Environment**
```bash
# Verify you're in Development environment
echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"
```

**Check 2: Configuration**
```bash
# Check appsettings.Development.json
cat appsettings.Development.json | grep -A 5 DemoMode
```

Expected output:
```json
"DemoMode": {
  "EnableDemoMode": true,
  ...
}
```

**Check 3: Application Logs**

Look for these log messages on startup:

✅ Success:
```
[Management API] Demo mode enabled. Seeding demo data...
[Management API] Creating demo integration: Demo: JSON to SOAP Order Processing
[Management API] Successfully created: Demo: JSON to SOAP Order Processing
...
[Management API] Demo data seeding completed successfully.
```

⚠️ Already Seeded:
```
[Management API] Demo data already exists. Skipping seeding. Set ForceReseed to true to override.
```

❌ Disabled:
```
[Management API] Demo mode is disabled or not in Development environment. Skipping demo data seeding.
```

### Demo Data Exists But Can't Test

**Issue**: Integrations created but QuickApiMapper.Web doesn't route to them

**Solution**: Ensure the Web API is configured to load integrations from the database:

1. Check that Web API and Management API are using the same database
2. Restart QuickApiMapper.Web after seeding
3. Verify the endpoint exists:
   ```bash
   GET http://localhost:5000/api/demo/fulfillment/submit
   ```

### Force Reseed Not Working

**Issue**: `ForceReseed: true` but data not recreated

**Cause**: Application might be caching the setting

**Solution**:
1. Stop the application completely
2. Update `appsettings.Development.json`
3. Delete any cached files in `bin/` and `obj/`
4. Restart the application

### Demo Services Not Accessible

**Issue**: `http://demo-soapapi/WarehouseService.asmx` returns 404

**Context**: The demo integrations reference demo services that need to be running

**Solution**: The destination URLs in demo integrations are placeholders. For full end-to-end testing:

1. Start the Demo.SoapApi project:
   ```bash
   cd src/Demo.SoapApi
   dotnet run
   ```

2. Update demo integration destination URLs to actual running services:
   ```bash
   PATCH https://localhost:7001/api/integrations/{id}
   {
     "destinationUrl": "http://localhost:5003/WarehouseService.asmx"
   }
   ```

3. Or use Aspire to orchestrate all services together (recommended)

## Integration with Aspire AppHost

For seamless demo experience, configure the Aspire AppHost to:

1. Enable demo mode on Management API
2. Start Demo.JsonApi and Demo.SoapApi services
3. Configure service discovery

Example `Program.cs` in AppHost:

```csharp
var managementApi = builder.AddProject<Projects.QuickApiMapper_Management_Api>("management-api")
    .WithEnvironment("DemoMode__EnableDemoMode", "true");

var demoJsonApi = builder.AddProject<Projects.Demo_JsonApi>("demo-jsonapi");
var demoSoapApi = builder.AddProject<Projects.Demo_SoapApi>("demo-soapapi");

var web = builder.AddProject<Projects.QuickApiMapper_Web>("web")
    .WithReference(managementApi)
    .WithReference(demoJsonApi)
    .WithReference(demoSoapApi);
```

## Related Documentation

- [DEMO_IMPLEMENTATION_PLAN.md](DEMO_IMPLEMENTATION_PLAN.md) - Complete demo architecture
- [Creating Integrations](articles/creating-integrations.md) - Manual integration setup
- [Field Mappings](articles/field-mappings.md) - Understanding JSONPath and XPath
- [Transformers](articles/transformers.md) - Available transformers and custom transformers
- [Message Capture](articles/message-capture.md) - Viewing transformation history

## Future Enhancements

The following features are planned for future demo data seeder improvements:

- **Sample Message Capture**: Pre-populate message history with successful and failed transformations
- **Realistic Timestamps**: Generate messages with varied timestamps for timeline visualization
- **Correlation IDs**: Add correlated message chains for tracking
- **Performance Metrics**: Seed with simulated processing times and statistics
- **Multiple Scenarios**: Add demo data for error cases, edge cases, and international orders
- **Interactive Demo Mode**: Guided walkthrough with step-by-step instructions in UI

## Support

For issues or questions about demo data:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Review application logs for detailed error messages
3. Verify database connectivity and migrations
4. Open an issue on GitHub with:
   - Environment (Development/Production)
   - Configuration settings
   - Log output
   - Steps to reproduce

---

**Last Updated**: 2026-01-10
**Version**: 1.0
**Applies To**: QuickApiMapper v1.0+
