# QuickApiMapper Demo Quick Reference Card

One-page cheat sheet with all essential information for the QuickApiMapper demo.

## Quick Start

```bash
# Clone and build
git clone https://github.com/your-org/QuickApiMapper.git
cd QuickApiMapper
dotnet build

# Start all services
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

## Service URLs

| Service | HTTP URL | HTTPS URL | Purpose |
|---------|----------|-----------|---------|
| **QuickApiMapper Web** | http://localhost:5000 | https://localhost:7000 | Transformation engine |
| **Management API** | http://localhost:7001 | https://localhost:7001 | Configuration management |
| **Designer Dashboard** | http://localhost:7002 | https://localhost:7002 | UI/monitoring |
| **Demo.JsonApi** | http://localhost:5100 | https://localhost:7100 | E-commerce API (source) |
| **Demo.SoapApi** | http://localhost:5101 | https://localhost:7101 | Warehouse SOAP (destination) |
| **Aspire Dashboard** | http://localhost:15000 | - | Service orchestration |
| **RabbitMQ Management** | http://localhost:15672 | - | Queue management (guest/guest) |

## Demo Endpoints

### JSON to SOAP Order Processing
```bash
POST http://localhost:5000/api/demo/fulfillment/submit
Content-Type: application/json
```

### Sample Order (Minimal)
```json
{
  "orderId": "ORD-TEST-001",
  "customerName": "Test User",
  "customerEmail": "TEST@EXAMPLE.COM",
  "orderDate": "2026-01-11T10:00:00Z",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [
    {
      "sku": "laptop-test",
      "productName": "Test Laptop",
      "quantity": 1,
      "unitPrice": 599.99
    }
  ],
  "shippingAddress": {
    "street": "123 Test St",
    "city": "Test City",
    "state": "TS",
    "postalCode": "00000",
    "country": "USA"
  },
  "priority": "STANDARD"
}
```

## Key Transformations

| Field | Input Example | Transformer | Output Example |
|-------|---------------|-------------|----------------|
| Customer Email | `JOHN@EXAMPLE.COM` | ToLower | `john@example.com` |
| Product SKU | `laptop-xps15` | ToUpper | `LAPTOP-XPS15` |
| Priority | `STANDARD` | MapValue | `STD` |
| Priority | `EXPRESS` | MapValue | `EXP` |
| Priority | `OVERNIGHT` | MapValue | `OVN` |

## cURL Commands

### Standard Order
```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-001",
    "customerName": "John Smith",
    "customerEmail": "JOHN.SMITH@EXAMPLE.COM",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 599.99,
    "currency": "USD",
    "items": [{"sku": "laptop-xps15", "productName": "Dell XPS 15", "quantity": 1, "unitPrice": 599.99}],
    "shippingAddress": {"street": "123 Main St", "city": "Seattle", "state": "WA", "postalCode": "98101", "country": "USA"},
    "priority": "STANDARD"
  }'
```

### Express Order
```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d @express-order.json
```

### List All Integrations
```bash
curl https://localhost:7001/api/integrations
```

### View Message History
Navigate to: https://localhost:7002 → Message History

## Demo Integrations

### 1. Demo: JSON to SOAP Order Processing
- **Endpoint**: `/api/demo/fulfillment/submit`
- **Source**: JSON
- **Destination**: SOAP
- **Field Mappings**: 16
- **Transformers**: 3 (ToLower, ToUpper, MapValue)
- **Use Case**: Primary demo integration

### 2. Demo: SOAP to JSON Fulfillment Status
- **Endpoint**: `/api/demo/fulfillment/status`
- **Source**: SOAP
- **Destination**: JSON
- **Field Mappings**: 5
- **Use Case**: Reverse transformation demo

### 3. Demo: RabbitMQ Order Batch Processing
- **Queue**: `quickapi.demo.orders`
- **Source**: JSON (via RabbitMQ)
- **Destination**: SOAP
- **Field Mappings**: 16 (same as #1)
- **Use Case**: Async/queue processing demo

## Common Tasks

### View Service Health
```bash
# QuickApiMapper Web
curl http://localhost:5000/health

# Management API
curl https://localhost:7001/health

# Designer Dashboard
curl https://localhost:7002/health

# Demo.JsonApi
curl https://localhost:7100/health

# Demo.SoapApi
curl http://localhost:5101/health
```

### View Demo Orders (Demo.JsonApi)
```bash
curl https://localhost:7100/api/orders
```

### View SOAP WSDL (Demo.SoapApi)
```bash
curl http://localhost:5101/WarehouseService.asmx?wsdl
```

### Reset Demo Data
1. Edit `src/QuickApiMapper.Management.Api/appsettings.Development.json`
2. Set `"ForceReseed": true`
3. Restart Management API
4. Set `"ForceReseed": false` (to prevent continuous re-seeding)

## Troubleshooting Quick Fixes

### Services Won't Start
```bash
# Check Docker is running
docker ps

# Restart Aspire AppHost
# Ctrl+C to stop
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

### Demo Data Not Seeded
```bash
# Check environment
echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"

# Force reseed
# Edit appsettings.Development.json: "ForceReseed": true
# Restart Management API
```

### Port Already in Use
```bash
# Windows - Find and kill process
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

### HTTPS Certificate Issues
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Dashboard Not Loading
- Try HTTP instead: http://localhost:7002
- Check Aspire Dashboard for service status
- Clear browser cache
- Restart designer-web service

### Message Not Captured
- Verify `EnableMessageCapture: true` in integration config
- Check PostgreSQL is running: `docker ps | grep postgres`
- Review Management API logs in Aspire Dashboard

## Field Mapping Reference

### JSONPath Syntax
| Pattern | Meaning | Example |
|---------|---------|---------|
| `$` | Root | `$.orderId` |
| `.` | Child | `$.customer.name` |
| `[]` | Array index | `$.items[0]` |
| `[*]` | All array elements | `$.items[*].sku` |
| `..` | Recursive descent | `$..email` |

### XPath Syntax (SOAP/XML)
| Pattern | Meaning | Example |
|---------|---------|---------|
| `/` | Root or child | `/OrderNumber` |
| `//` | Anywhere | `//CustomerInfo/Name` |
| `[@attr]` | Attribute | `/Item[@id]` |
| `[1]` | First element | `/Items/Item[1]` |

## Priority Codes

| JSON Value | SOAP Code | Transformer |
|------------|-----------|-------------|
| `STANDARD` | `STD` | MapValue |
| `EXPRESS` | `EXP` | MapValue |
| `OVERNIGHT` | `OVN` | MapValue |
| _Unknown_ | `STD` | MapValue (fallback) |

## Expected Performance

| Metric | Target | Typical |
|--------|--------|---------|
| Processing Time | < 300ms | 50-200ms |
| Success Rate | > 99% | 100% (demo) |
| Throughput | > 100 req/s | Varies |
| Message Capture Overhead | < 10ms | ~5ms |

## Key Features Demonstrated

- ✅ JSON to SOAP transformation
- ✅ 16 field mappings (JSONPath → XPath)
- ✅ 3 transformers (ToLower, ToUpper, MapValue)
- ✅ SOAP envelope construction
- ✅ Array handling (`items[*]`)
- ✅ Message capture and audit
- ✅ Real-time monitoring dashboard
- ✅ Sub-200ms performance
- ✅ Visual configuration (no code)
- ✅ Production-ready observability

## Presentation Key Messages

1. **No Code Required**: All configuration via visual interface
2. **Production-Ready**: Sub-200ms latency, full observability
3. **Flexible**: Works with JSON, SOAP, XML, message queues
4. **Extensible**: Drop-in custom transformers
5. **Unified Platform**: One tool for all integrations

## Next Steps

1. **Try It**: Clone repo, run `dotnet run`, submit orders
2. **Read Docs**: [DEMO_GUIDE.md](DEMO_GUIDE.md), [API_SAMPLES.md](API_SAMPLES.md)
3. **Schedule Deep-Dive**: Technical session for your use case
4. **Join Community**: GitHub Discussions, contribute ideas

## Documentation Links

- **Main Demo Guide**: [DEMO_GUIDE.md](DEMO_GUIDE.md)
- **API Samples**: [API_SAMPLES.md](API_SAMPLES.md)
- **Architecture**: [ARCHITECTURE_DEMO.md](ARCHITECTURE_DEMO.md)
- **Presentation Script**: [DEMO_PRESENTATION_SCRIPT.md](DEMO_PRESENTATION_SCRIPT.md)
- **FAQ**: [DEMO_FAQ.md](DEMO_FAQ.md)
- **Quick Start**: [../DEMO_QUICK_START.md](../DEMO_QUICK_START.md)
- **Main README**: [../README.md](../README.md)

## Support

- **GitHub Issues**: https://github.com/your-org/QuickApiMapper/issues
- **Documentation**: https://your-org.github.io/QuickApiMapper
- **Email**: support@example.com

---

**Version**: 1.0 | **Updated**: 2026-01-11 | **Format**: Quick Reference Card
