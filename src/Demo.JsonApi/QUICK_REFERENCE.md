# Demo.JsonApi - Quick Reference Card

## Quick Start
```bash
cd src/Demo.JsonApi
dotnet run
# API: https://localhost:7100
# Docs: https://localhost:7100/scalar/v1
```

## Endpoints

| Method | Endpoint | Description | Status Codes |
|--------|----------|-------------|--------------|
| POST | `/api/orders` | Create order | 201, 409, 500 |
| GET | `/api/orders` | List orders | 200 |
| GET | `/api/orders/{id}` | Get order | 200, 404 |
| PUT | `/api/orders/{id}/status` | Update status | 200, 404 |
| GET | `/health` | Health check | 200 |

## Sample Requests

### Create Order
```bash
curl -X POST https://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Test User",
    "customerEmail": "test@example.com",
    "totalAmount": 99.99,
    "currency": "USD",
    "items": [{"sku": "TEST-001", "productName": "Test Product", "quantity": 1, "unitPrice": 99.99}],
    "shippingAddress": {"street": "123 St", "city": "City", "state": "ST", "postalCode": "12345", "country": "USA"},
    "priority": "STANDARD"
  }'
```

### Get All Orders
```bash
curl https://localhost:7100/api/orders
```

### Get Order
```bash
curl https://localhost:7100/api/orders/ORD-2026-001
```

### Update Status
```bash
curl -X PUT https://localhost:7100/api/orders/ORD-2026-001/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Shipped", "notes": "Via FedEx"}'
```

## Order Statuses
- `Pending` - Awaiting processing
- `Processing` - Being prepared
- `Shipped` - In transit
- `Delivered` - Completed
- `Cancelled` - Cancelled

## Priority Levels
- `STANDARD` - 5-7 business days
- `EXPRESS` - 2-3 business days
- `OVERNIGHT` - Next day delivery

## Pre-Seeded Orders
- `ORD-2026-001` to `ORD-2026-010`
- Various products and statuses
- Ready for immediate testing

## Order Model
```json
{
  "orderId": "string (auto-generated)",
  "customerName": "string (required)",
  "customerEmail": "string (required)",
  "orderDate": "datetime (auto-set)",
  "totalAmount": "decimal (required)",
  "currency": "string (default: USD)",
  "items": [
    {
      "sku": "string",
      "productName": "string",
      "quantity": "int",
      "unitPrice": "decimal"
    }
  ],
  "shippingAddress": {
    "street": "string",
    "city": "string",
    "state": "string",
    "postalCode": "string",
    "country": "string"
  },
  "priority": "string (STANDARD|EXPRESS|OVERNIGHT)",
  "status": "enum (auto-set to Pending)"
}
```

## Configuration Files
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings
- `launchSettings.json` - Launch profiles

## Ports
- HTTP: 5100
- HTTPS: 7100

## With Aspire
```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
# Access Aspire Dashboard for all services
```

## Health Checks
- `/health` - Simple health check
- `/alive` - Liveness probe (Aspire)
- `/ready` - Readiness probe (Aspire)

## Project Files
```
Demo.JsonApi/
├── Models/
│   ├── Order.cs
│   └── OrderStatusUpdate.cs
├── Services/
│   ├── IOrderService.cs
│   └── InMemoryOrderService.cs
├── Examples/
│   ├── sample-integration.json
│   ├── example-order-payload.json
│   └── expected-soap-output.xml
├── Program.cs
└── Demo.JsonApi.csproj
```

## Dependencies
- .NET 10
- Microsoft.AspNetCore.OpenApi
- Scalar.AspNetCore
- QuickApiMapper.Host.ServiceDefaults

## Common Tasks

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Test Endpoint
```bash
curl https://localhost:7100/api/orders/ORD-2026-001
```

### View Logs
Logs output to console with structured logging

### Stop
`Ctrl+C` in terminal

## Integration with QuickApiMapper

1. Place `sample-integration.json` in QuickApiMapper's integrations folder
2. Restart QuickApiMapper
3. Send order to QuickApiMapper endpoint
4. QuickApiMapper transforms JSON → SOAP

## Troubleshooting

### Port in Use
Change ports in `launchSettings.json`

### HTTPS Certificate
```bash
dotnet dev-certs https --trust
```

### Orders Not Persisting
Expected - uses in-memory storage

## Example Integration Flow
```
Demo.JsonApi (JSON)
    → QuickApiMapper (Transform)
        → Legacy SOAP Service
```

## Resources
- [README.md](README.md) - Full documentation
- [USAGE.md](USAGE.md) - Usage guide
- [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) - Complete overview
- Scalar Docs: https://localhost:7100/scalar/v1

## Support
GitHub: https://github.com/jerrettdavis/QuickApiMapper

---
**Version**: 1.0.0 | **Framework**: .NET 10 | **License**: MIT
