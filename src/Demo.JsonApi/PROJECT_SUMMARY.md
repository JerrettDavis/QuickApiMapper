# Demo.JsonApi - Project Summary

## Overview

Demo.JsonApi is a complete, production-ready ASP.NET Core Minimal API demonstrating a modern e-commerce order management system. It serves as a reference implementation for testing QuickApiMapper's JSON-to-SOAP transformation capabilities.

## Project Details

- **Framework**: .NET 10 (ASP.NET Core Minimal APIs)
- **Architecture**: Clean, layered architecture with separation of concerns
- **API Style**: RESTful with comprehensive OpenAPI documentation
- **Storage**: In-memory (ConcurrentDictionary) for development/testing
- **Observability**: Full Aspire integration with health checks and telemetry
- **Documentation**: Scalar-powered interactive API docs

## Project Structure

```
Demo.JsonApi/
├── Models/                          # Domain models
│   ├── Order.cs                     # Main order entity
│   └── OrderStatusUpdate.cs         # Status update request DTO
├── Services/                        # Business logic
│   ├── IOrderService.cs            # Service interface
│   └── InMemoryOrderService.cs     # In-memory implementation
├── Examples/                        # Integration examples
│   ├── sample-integration.json     # QuickApiMapper mapping config
│   ├── example-order-payload.json  # Sample order payload
│   └── expected-soap-output.xml    # Expected SOAP transformation
├── Properties/
│   └── launchSettings.json         # Launch configuration
├── Program.cs                       # API endpoints and configuration
├── Demo.JsonApi.csproj             # Project file
├── appsettings.json                # Configuration
├── appsettings.Development.json    # Development configuration
├── README.md                        # Project documentation
├── USAGE.md                         # Usage and integration guide
└── PROJECT_SUMMARY.md              # This file
```

## Key Features

### 1. RESTful API Design

Five main endpoints following REST conventions:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/orders` | POST | Create new order |
| `/api/orders` | GET | List all orders |
| `/api/orders/{id}` | GET | Get specific order |
| `/api/orders/{id}/status` | PUT | Update order status |
| `/health` | GET | Health check |

### 2. Rich Order Model

Complete e-commerce order structure including:
- Customer information (name, email)
- Order details (ID, date, total, currency)
- Line items with SKUs and pricing
- Shipping address
- Order priority (STANDARD, EXPRESS, OVERNIGHT)
- Order status tracking (Pending, Processing, Shipped, Delivered, Cancelled)

### 3. Pre-Seeded Sample Data

10 realistic orders with:
- Various product categories (electronics, accessories, home theater)
- Different order values ($299 - $4,999)
- Multiple order statuses
- Diverse US shipping addresses
- Different priority levels

### 4. Interactive Documentation

Scalar-powered API documentation accessible at `/scalar/v1` featuring:
- Interactive endpoint testing
- Complete request/response schemas
- Example payloads
- Error code documentation

### 5. Aspire Integration

Full .NET Aspire ServiceDefaults integration providing:
- Health checks (`/health`, `/alive`, `/ready`)
- OpenTelemetry metrics and tracing
- Service discovery support
- Structured logging

### 6. Thread-Safe Storage

ConcurrentDictionary-based in-memory storage ensuring:
- Thread-safe concurrent operations
- Fast read/write performance
- Automatic ID generation
- No external dependencies

## API Endpoints Detail

### POST /api/orders
**Purpose**: Submit a new order

**Request Body**:
```json
{
  "customerName": "John Smith",
  "customerEmail": "john.smith@example.com",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [{
    "sku": "LAPTOP-XPS15",
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
}
```

**Response**: 201 Created with order details including generated `orderId`

### GET /api/orders
**Purpose**: List all orders (newest first)

**Response**: 200 OK with array of all orders

### GET /api/orders/{id}
**Purpose**: Get a specific order

**Response**: 200 OK with order details, or 404 Not Found

### PUT /api/orders/{id}/status
**Purpose**: Update order status

**Request Body**:
```json
{
  "status": "Shipped",
  "notes": "Shipped via FedEx Ground"
}
```

**Response**: 200 OK with updated order, or 404 Not Found

### GET /health
**Purpose**: Simple health check

**Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-10T21:30:00Z",
  "service": "Demo.JsonApi",
  "version": "1.0.0"
}
```

## Integration with QuickApiMapper

### Scenario: JSON to SOAP Transformation

Demo.JsonApi sends modern JSON orders → QuickApiMapper transforms → Legacy SOAP service receives

**Mapping Configuration**: See `Examples/sample-integration.json`

**Key Mappings**:
- `$.orderId` → `/soap:Envelope/soap:Body/SubmitOrder/OrderID`
- `$.customerName` → `/soap:Envelope/soap:Body/SubmitOrder/CustomerName`
- `$.items[0].sku` → `/soap:Envelope/soap:Body/SubmitOrder/Items/Item[1]/SKU`
- `$.shippingAddress.city` → `/soap:Envelope/soap:Body/SubmitOrder/ShippingAddress/City`
- And more...

## Running the Project

### Standalone
```bash
cd src/Demo.JsonApi
dotnet run
```
Access at: http://localhost:5100 or https://localhost:7100

### With Aspire AppHost
```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```
All services orchestrated together with infrastructure

## Sample Orders

Pre-seeded orders include:
- **ORD-2026-001**: Dell XPS 15 ($599.99) - Delivered
- **ORD-2026-002**: iPhone 15 Pro Bundle ($1,299.97) - Shipped
- **ORD-2026-003**: Sony Headphones ($449.99) - Processing
- **ORD-2026-004**: iPad Pro Bundle ($2,499.99) - Processing
- **ORD-2026-005**: Apple Watch Series 9 ($799.99) - Pending
- **ORD-2026-006**: MacBook Pro 16" ($3,499.98) - Pending
- **ORD-2026-007**: Sonos Speakers ($549.99) - Pending
- **ORD-2026-008**: LG Monitor Setup ($1,899.97) - Pending
- **ORD-2026-009**: Gaming Keyboard Bundle ($299.99) - Pending
- **ORD-2026-010**: LG OLED TV Setup ($4,999.95) - Pending

## Testing the API

### Using curl
```bash
# Get all orders
curl https://localhost:7100/api/orders

# Get specific order
curl https://localhost:7100/api/orders/ORD-2026-001

# Create new order
curl -X POST https://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d @Examples/example-order-payload.json

# Update status
curl -X PUT https://localhost:7100/api/orders/ORD-2026-001/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Shipped", "notes": "Shipped via UPS"}'
```

### Using Scalar UI
1. Navigate to https://localhost:7100/scalar/v1
2. Explore endpoints interactively
3. Test directly from the browser
4. View real-time request/response data

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10 |
| API Style | ASP.NET Core Minimal APIs |
| Documentation | Scalar (OpenAPI 3.0) |
| Observability | .NET Aspire ServiceDefaults |
| Logging | Microsoft.Extensions.Logging |
| Storage | In-Memory (ConcurrentDictionary) |
| Serialization | System.Text.Json |
| Health Checks | ASP.NET Core Health Checks |

## Dependencies

### NuGet Packages
- `Microsoft.AspNetCore.OpenApi` - OpenAPI document generation
- `Scalar.AspNetCore` - Interactive API documentation UI

### Project References
- `QuickApiMapper.Host.ServiceDefaults` - Aspire integration

## Configuration

### Application Settings

**appsettings.json**:
- Default logging levels
- AllowedHosts configuration

**appsettings.Development.json**:
- Enhanced debug logging
- Development-specific settings

**launchSettings.json**:
- HTTP: port 5100
- HTTPS: port 7100
- Launch URL: `/scalar/v1`

## Design Patterns

1. **Dependency Injection**: Services registered and injected via DI container
2. **Interface Segregation**: Clean service interfaces (`IOrderService`)
3. **Repository Pattern**: Service layer abstracts storage details
4. **DTO Pattern**: Separate request/response models (`OrderStatusUpdate`)
5. **Factory Pattern**: Automatic order ID generation

## Error Handling

- **400 Bad Request**: Invalid input data
- **404 Not Found**: Order not found
- **409 Conflict**: Duplicate order ID
- **500 Internal Server Error**: Unexpected errors with details

## Future Enhancements

Potential additions for production use:
- Database persistence (Entity Framework Core)
- Pagination for large result sets
- Filtering and search capabilities
- Authentication and authorization
- Rate limiting
- Order cancellation workflow
- Payment processing simulation
- Inventory management
- Customer management endpoints
- Order history and audit trail
- Email notifications

## Performance Characteristics

- **In-Memory Storage**: Sub-millisecond response times
- **Thread-Safe**: Concurrent request handling via ConcurrentDictionary
- **Stateless**: Horizontally scalable (with external storage)
- **Minimal Overhead**: No ORM or database latency

## Security Considerations

Current implementation is for **development/testing only**:
- No authentication/authorization
- No input sanitization beyond model validation
- No rate limiting
- HTTPS recommended but not enforced

For production, add:
- JWT or OAuth authentication
- Role-based authorization
- Input validation and sanitization
- Rate limiting and throttling
- CORS configuration
- API key management

## Compliance and Best Practices

- **RESTful Design**: Follows REST architectural principles
- **HTTP Semantics**: Proper use of status codes and methods
- **OpenAPI 3.0**: Complete API specification
- **Clean Code**: Well-organized, documented, and testable
- **.NET Guidelines**: Follows Microsoft's recommended patterns
- **Nullable Reference Types**: Enabled for null safety

## Monitoring and Observability

Via Aspire ServiceDefaults:
- **Metrics**: Request counts, duration, error rates
- **Tracing**: Distributed tracing with OpenTelemetry
- **Logging**: Structured logging to console and external sinks
- **Health Checks**: Liveness and readiness probes

## Development Experience

- **Hot Reload**: Full support for code changes without restart
- **IntelliSense**: Complete with XML documentation
- **Debugging**: Full debugging support in Visual Studio/Rider/VS Code
- **Testing**: Easy to test with in-memory storage

## Related Documentation

- [README.md](README.md) - Project overview and features
- [USAGE.md](USAGE.md) - Detailed usage guide and examples
- [Examples/](Examples/) - Sample integrations and payloads

## License

MIT - Part of the QuickApiMapper project

## Support and Contribution

Part of the QuickApiMapper ecosystem:
- Repository: https://github.com/jerrettdavis/QuickApiMapper
- Issues: Use main repository issue tracker
- Discussions: Use main repository discussions

## Version History

- **1.0.0** (2026-01-10): Initial release
  - Complete REST API implementation
  - 10 pre-seeded sample orders
  - Aspire integration
  - Scalar documentation
  - Example QuickApiMapper integration configs

---

**Created**: January 10, 2026
**Author**: QuickApiMapper Project
**Purpose**: Demonstration and testing of JSON-to-SOAP transformation
**Status**: Production-ready for demo/testing purposes
