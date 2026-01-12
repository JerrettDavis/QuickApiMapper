# QuickApiMapper Demo - Quick Start Guide

Get up and running with QuickApiMapper demo in under 5 minutes.

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running (for Aspire orchestration)
- Visual Studio 2022, Rider, or VS Code

## Step 1: Clone and Build

```bash
git clone <repository-url>
cd QuickApiMapper
dotnet restore
dotnet build
```

## Step 2: Enable Demo Mode

Demo mode is **already enabled** in Development by default. Verify in `src/QuickApiMapper.Management.Api/appsettings.Development.json`:

```json
{
  "DemoMode": {
    "EnableDemoMode": true
  }
}
```

## Step 3: Start the Application

### Option A: Using Aspire (Recommended)

```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

This starts all services:
- Management API (port 7001)
- QuickApiMapper Web (port 5000)
- Designer Dashboard (port 7002)
- Demo JSON API (port 5003)
- Demo SOAP API (port 5004)

### Option B: Individual Projects

**Terminal 1 - Management API** (with demo seeding):
```bash
cd src/QuickApiMapper.Management.Api
dotnet run
```

**Terminal 2 - QuickApiMapper Web**:
```bash
cd src/QuickApiMapper.Web
dotnet run
```

**Terminal 3 - Designer Dashboard**:
```bash
cd src/QuickApiMapper.Designer.Web
dotnet run
```

## Step 4: Verify Demo Data

Check that demo integrations were created:

```bash
curl https://localhost:7001/api/integrations
```

Expected response includes:
- Demo: JSON to SOAP Order Processing
- Demo: SOAP to JSON Fulfillment Status
- Demo: RabbitMQ Order Batch Processing

## Step 5: Test the Demo

### Send a Test Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-001",
    "customerName": "John Smith",
    "customerEmail": "JOHN.SMITH@EXAMPLE.COM",
    "orderDate": "2026-01-10T14:30:00Z",
    "totalAmount": 599.99,
    "currency": "USD",
    "items": [{
      "sku": "laptop-xps15",
      "productName": "Dell XPS 15 Laptop",
      "quantity": 1,
      "unitPrice": 599.99
    }],
    "shippingAddress": {
      "street": "123 Main St",
      "city": "Seattle",
      "state": "WA",
      "postalCode": "98101",
      "country": "USA"
    },
    "priority": "STANDARD"
  }'
```

### View the Transformation

1. Open Designer Dashboard: https://localhost:7002
2. Navigate to "Message Capture" or "History"
3. See the JSON input transformed to SOAP output
4. Observe transformations:
   - Email: `JOHN.SMITH@EXAMPLE.COM` → `john.smith@example.com`
   - SKU: `laptop-xps15` → `LAPTOP-XPS15`
   - Priority: `STANDARD` → `STD`

## What's Included in Demo Mode

The demo seeder automatically creates:

1. **JSON to SOAP Order Processing** - Main demo integration
   - 16 field mappings
   - 3 transformers (ToLower, ToUpper, MapValue)
   - Complete SOAP envelope configuration

2. **SOAP to JSON Status Updates** - Reverse integration
   - Shows SOAP-to-JSON transformation
   - 5 field mappings

3. **RabbitMQ Batch Processing** - Queue-based integration
   - Demonstrates async message processing
   - Same mappings as #1

## Next Steps

### Explore the Designer

Navigate to https://localhost:7002 and:
- View integration configurations
- Test transformations in real-time
- Inspect message capture history
- Modify field mappings

### Try Different Scenarios

**Express Shipping**:
```json
{
  "orderId": "ORD-2026-002",
  "priority": "EXPRESS",
  ...
}
```
Results in `<PriorityCode>EXP</PriorityCode>`

**Overnight Shipping**:
```json
{
  "orderId": "ORD-2026-003",
  "priority": "OVERNIGHT",
  ...
}
```
Results in `<PriorityCode>OVN</PriorityCode>`

### View in Database

**SQLite** (default):
```bash
sqlite3 src/QuickApiMapper.Management.Api/quickapimapper.db

SELECT Name, Endpoint, SourceType, DestinationType
FROM integrationmappings
WHERE Name LIKE 'Demo:%';
```

### Reset Demo Data

To start fresh:

1. **Update config**:
   ```json
   {
     "DemoMode": {
       "EnableDemoMode": true,
       "ForceReseed": true
     }
   }
   ```

2. **Restart application** - Demo data will be recreated

3. **Reset config**:
   ```json
   {
     "DemoMode": {
       "EnableDemoMode": true,
       "ForceReseed": false
     }
   }
   ```

## Troubleshooting

### Demo data not created?

Check logs for:
```
[Management API] Demo mode enabled. Seeding demo data...
[Management API] Demo data seeding completed successfully.
```

If you see "Demo mode is disabled", verify:
- `ASPNETCORE_ENVIRONMENT=Development`
- `DemoMode.EnableDemoMode=true` in appsettings.Development.json

### Can't reach endpoints?

Verify services are running:
```bash
# Management API
curl https://localhost:7001/health

# QuickApiMapper Web
curl http://localhost:5000/health

# Designer Dashboard
curl https://localhost:7002
```

### Database locked?

Stop all running instances:
```bash
# Kill all dotnet processes
pkill dotnet

# Or on Windows:
taskkill /F /IM dotnet.exe
```

## Documentation

- **[DEMO_DATA.md](docs/DEMO_DATA.md)** - Complete demo data documentation
- **[DEMO_IMPLEMENTATION_PLAN.md](docs/DEMO_IMPLEMENTATION_PLAN.md)** - Architecture and implementation details
- **[Creating Integrations](docs/articles/creating-integrations.md)** - Manual integration setup
- **[Field Mappings](docs/articles/field-mappings.md)** - JSONPath and XPath guide
- **[Transformers](docs/articles/transformers.md)** - Available transformers

## Support

Questions? Issues?
1. Check [DEMO_DATA.md](docs/DEMO_DATA.md) troubleshooting section
2. Review application logs
3. Open an issue on GitHub

---

**Ready to build your own integrations?**

Disable demo mode and create custom integrations:

```json
{
  "DemoMode": {
    "EnableDemoMode": false
  }
}
```

Then follow the [Creating Integrations](docs/articles/creating-integrations.md) guide.
