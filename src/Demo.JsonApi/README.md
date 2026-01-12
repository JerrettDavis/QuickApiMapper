# Demo.JsonApi - E-Commerce Order API

A modern ASP.NET Core Minimal API demonstrating a RESTful e-commerce order management system built with .NET 10.

## Overview

This demo API simulates a real-world e-commerce order processing system that can be used to test the QuickApiMapper's JSON-to-SOAP transformation capabilities. It provides a clean, well-documented API for managing customer orders.

## Features

- **RESTful API Design**: Clean, intuitive endpoints following REST best practices
- **In-Memory Storage**: Fast, thread-safe concurrent dictionary for development and testing
- **OpenAPI/Swagger**: Interactive API documentation powered by Scalar
- **Aspire Integration**: Built-in observability, health checks, and service discovery
- **Pre-Seeded Data**: 10 realistic sample orders for immediate testing
- **.NET 10**: Built on the latest .NET framework

## API Endpoints

### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/orders` | Submit a new order |
| `GET` | `/api/orders` | List all orders (newest first) |
| `GET` | `/api/orders/{id}` | Get a specific order by ID |
| `PUT` | `/api/orders/{id}/status` | Update order status |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Simple health check |
| `GET` | `/health` (Aspire) | Detailed health endpoint |
| `GET` | `/alive` | Liveness probe |
| `GET` | `/ready` | Readiness probe |

## Order Model

```json
{
  "orderId": "ORD-2026-001",
  "customerName": "John Smith",
  "customerEmail": "john.smith@example.com",
  "orderDate": "2026-01-10T14:30:00Z",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [
    {
      "sku": "LAPTOP-XPS15",
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
  "priority": "STANDARD",
  "status": "Pending"
}
```

## Order Status Values

- `Pending` - Order received, awaiting processing
- `Processing` - Order is being prepared
- `Shipped` - Order has been shipped
- `Delivered` - Order delivered to customer
- `Cancelled` - Order cancelled

## Priority Levels

- `STANDARD` - Regular shipping (5-7 business days)
- `EXPRESS` - Expedited shipping (2-3 business days)
- `OVERNIGHT` - Next day delivery

## Running the API

### Using .NET CLI

```bash
cd src/Demo.JsonApi
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5100
- HTTPS: https://localhost:7100

### Using Aspire AppHost

The Demo.JsonApi can be integrated into the QuickApiMapper.Host.AppHost for orchestrated deployment with other services.

## API Documentation

Once running, visit the Scalar API documentation at:
- https://localhost:7100/scalar/v1

This provides an interactive interface to explore and test all endpoints.

## Sample Usage

### Create a New Order

```bash
curl -X POST https://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Test Customer",
    "customerEmail": "test@example.com",
    "totalAmount": 99.99,
    "currency": "USD",
    "items": [
      {
        "sku": "TEST-001",
        "productName": "Test Product",
        "quantity": 1,
        "unitPrice": 99.99
      }
    ],
    "shippingAddress": {
      "street": "123 Test St",
      "city": "Test City",
      "state": "TS",
      "postalCode": "12345",
      "country": "USA"
    },
    "priority": "STANDARD"
  }'
```

### Get All Orders

```bash
curl https://localhost:7100/api/orders
```

### Get Specific Order

```bash
curl https://localhost:7100/api/orders/ORD-2026-001
```

### Update Order Status

```bash
curl -X PUT https://localhost:7100/api/orders/ORD-2026-001/status \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Shipped",
    "notes": "Shipped via UPS Ground"
  }'
```

## Integration with QuickApiMapper

This API is designed to work seamlessly with QuickApiMapper for transforming JSON orders to SOAP messages. Example integration:

1. Configure QuickApiMapper with a JSON-to-SOAP mapping
2. Point the source to Demo.JsonApi endpoints
3. Map order fields to SOAP envelope elements
4. Forward transformed messages to legacy SOAP services

## Pre-Seeded Orders

The API includes 10 sample orders with IDs from `ORD-2026-001` to `ORD-2026-010`, featuring:
- Various product categories (electronics, accessories)
- Different order statuses and priorities
- Diverse shipping addresses across the US
- Order amounts ranging from $299 to $4,999

## Technology Stack

- **Framework**: .NET 10 (ASP.NET Core Minimal APIs)
- **Documentation**: Scalar (OpenAPI)
- **Observability**: Aspire ServiceDefaults
- **Storage**: In-Memory (ConcurrentDictionary)
- **Architecture**: Clean, minimal, modern RESTful design

## Project Structure

```
Demo.JsonApi/
├── Models/
│   ├── Order.cs              # Main order model
│   └── OrderStatusUpdate.cs  # Status update request model
├── Services/
│   ├── IOrderService.cs      # Service interface
│   └── InMemoryOrderService.cs # In-memory implementation
├── Properties/
│   └── launchSettings.json   # Launch configuration
├── Program.cs                # API endpoints and configuration
├── appsettings.json          # Configuration
└── Demo.JsonApi.csproj       # Project file
```

## Contributing

This is a demonstration project. Feel free to extend it with:
- Additional order filtering/search capabilities
- Pagination for large result sets
- Order cancellation workflows
- Payment processing simulation
- Inventory management
- Customer management endpoints

## License

MIT - Part of the QuickApiMapper project
