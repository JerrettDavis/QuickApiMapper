# Demo.JsonApi Usage Guide

This guide shows how to use the Demo.JsonApi with QuickApiMapper to transform JSON orders to SOAP messages.

## Quick Start

### 1. Run the Demo API

```bash
cd src/Demo.JsonApi
dotnet run
```

The API will start on:
- HTTP: http://localhost:5100
- HTTPS: https://localhost:7100

### 2. Explore the API

Open your browser to: https://localhost:7100/scalar/v1

This opens the interactive Scalar API documentation where you can explore and test all endpoints.

### 3. Test the API

#### Get All Pre-Seeded Orders

```bash
curl https://localhost:7100/api/orders
```

You should see 10 sample orders (ORD-2026-001 through ORD-2026-010).

#### Get a Specific Order

```bash
curl https://localhost:7100/api/orders/ORD-2026-001
```

#### Create a New Order

```bash
curl -X POST https://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d @Examples/example-order-payload.json
```

#### Update Order Status

```bash
curl -X PUT https://localhost:7100/api/orders/ORD-2026-001/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Shipped", "notes": "Shipped via FedEx"}'
```

## Integration with QuickApiMapper

### Option 1: Using Aspire AppHost (Recommended)

Run both Demo.JsonApi and QuickApiMapper together:

```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

This will launch:
- Demo.JsonApi (demo-jsonapi)
- QuickApiMapper.Web (web-api)
- QuickApiMapper.Management.Api (management-api)
- QuickApiMapper.Designer.Web (designer-web)
- Supporting infrastructure (PostgreSQL, Redis, RabbitMQ)

Access the Aspire Dashboard at the URL shown in the console to see all running services.

### Option 2: Manual Setup

#### Step 1: Start Demo.JsonApi

```bash
cd src/Demo.JsonApi
dotnet run
```

#### Step 2: Start QuickApiMapper

```bash
cd src/QuickApiMapper.Web
dotnet run
```

#### Step 3: Configure the Integration

Copy the sample integration configuration:

```bash
cp src/Demo.JsonApi/Examples/sample-integration.json \
   src/QuickApiMapper.Web/Integrations/demo-order-to-soap.json
```

Or use the Management API or Designer Web UI to create the integration mapping.

#### Step 4: Test the Integration

Send an order from Demo.JsonApi through QuickApiMapper:

```bash
# Get an order from Demo.JsonApi
curl https://localhost:7100/api/orders/ORD-2026-001 > order.json

# Send it through QuickApiMapper for transformation
curl -X POST http://localhost:5000/api/demo/order-to-soap \
  -H "Content-Type: application/json" \
  -d @order.json
```

QuickApiMapper will transform the JSON order to SOAP format and forward it to the configured destination.

## Example Integration Workflow

### Scenario: Legacy SOAP Order System Integration

You have a legacy SOAP-based order management system that needs to accept orders from modern JSON REST APIs.

**Before QuickApiMapper:**
- Write custom transformation code
- Maintain SOAP client libraries
- Handle versioning and changes manually

**With QuickApiMapper + Demo.JsonApi:**

1. **Configure the Mapping** (in Designer Web UI or Management API):
   - Source: JSON from Demo.JsonApi
   - Destination: SOAP to legacy system
   - Map fields using visual designer

2. **Deploy**:
   - QuickApiMapper handles transformation
   - No code changes needed
   - Update mappings without redeployment

3. **Monitor**:
   - View transformed messages in real-time
   - Check health endpoints
   - Use telemetry for debugging

### Sample Mapping

The `Examples/sample-integration.json` file shows a complete mapping from Demo.JsonApi's JSON format to a SOAP envelope:

**JSON Input** (from Demo.JsonApi):
```json
{
  "orderId": "ORD-2026-001",
  "customerName": "John Smith",
  "customerEmail": "john.smith@example.com",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [...],
  "shippingAddress": {...}
}
```

**SOAP Output** (to legacy system):
```xml
<soap:Envelope>
  <soap:Body>
    <SubmitOrder>
      <OrderID>ORD-2026-001</OrderID>
      <CustomerName>John Smith</CustomerName>
      <CustomerEmail>john.smith@example.com</CustomerEmail>
      ...
    </SubmitOrder>
  </soap:Body>
</soap:Envelope>
```

## Testing Different Scenarios

### Test Order Status Flow

1. Create a new order (Pending):
```bash
curl -X POST https://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d @Examples/example-order-payload.json
```

2. Update to Processing:
```bash
curl -X PUT https://localhost:7100/api/orders/ORD-2026-XXXX/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Processing"}'
```

3. Update to Shipped:
```bash
curl -X PUT https://localhost:7100/api/orders/ORD-2026-XXXX/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Shipped"}'
```

4. Verify the order:
```bash
curl https://localhost:7100/api/orders/ORD-2026-XXXX
```

### Test Different Order Types

**High-Value Order:**
```json
{
  "customerName": "VIP Customer",
  "customerEmail": "vip@example.com",
  "totalAmount": 9999.99,
  "currency": "USD",
  "items": [{
    "sku": "PREMIUM-001",
    "productName": "Premium Product Bundle",
    "quantity": 1,
    "unitPrice": 9999.99
  }],
  "shippingAddress": {...},
  "priority": "OVERNIGHT"
}
```

**Multi-Item Order:**
```json
{
  "customerName": "Bulk Customer",
  "customerEmail": "bulk@example.com",
  "totalAmount": 499.95,
  "currency": "USD",
  "items": [
    {"sku": "ITEM-001", "productName": "Product 1", "quantity": 3, "unitPrice": 99.99},
    {"sku": "ITEM-002", "productName": "Product 2", "quantity": 2, "unitPrice": 99.99}
  ],
  "shippingAddress": {...},
  "priority": "STANDARD"
}
```

**International Order:**
```json
{
  "customerName": "International Customer",
  "customerEmail": "intl@example.com",
  "totalAmount": 599.99,
  "currency": "EUR",
  "items": [{...}],
  "shippingAddress": {
    "street": "123 Main Street",
    "city": "London",
    "state": "Greater London",
    "postalCode": "SW1A 1AA",
    "country": "United Kingdom"
  },
  "priority": "EXPRESS"
}
```

## Health Checks

The Demo.JsonApi includes multiple health check endpoints:

### Simple Health Check
```bash
curl https://localhost:7100/health
```

Returns:
```json
{
  "status": "Healthy",
  "timestamp": "2026-01-10T21:30:00Z",
  "service": "Demo.JsonApi",
  "version": "1.0.0"
}
```

### Aspire Health Endpoints

When running via Aspire AppHost:

```bash
# Liveness probe
curl https://localhost:7100/alive

# Readiness probe
curl https://localhost:7100/ready

# Detailed health
curl https://localhost:7100/health
```

## API Performance Testing

### Load Testing with curl

Create multiple orders rapidly:

```bash
for i in {1..10}; do
  curl -X POST https://localhost:7100/api/orders \
    -H "Content-Type: application/json" \
    -d @Examples/example-order-payload.json &
done
wait
```

### Verify Order Count

```bash
curl https://localhost:7100/api/orders | jq 'length'
```

## Troubleshooting

### API Not Starting

Check port availability:
```bash
netstat -ano | findstr :5100
netstat -ano | findstr :7100
```

### Orders Not Persisting

This is expected behavior! The Demo.JsonApi uses in-memory storage. All data is lost when the application stops. This is intentional for demo/testing purposes.

### HTTPS Certificate Issues

Trust the development certificate:
```bash
dotnet dev-certs https --trust
```

## Next Steps

1. **Explore the Designer**: Use QuickApiMapper.Designer.Web to visually create mappings
2. **Test Transformations**: Send Demo.JsonApi orders through QuickApiMapper
3. **Monitor Messages**: Use the Message Capture feature to view transformed messages
4. **Extend the Demo**: Add new endpoints or modify the Order model
5. **Production Setup**: Replace in-memory storage with database persistence

## Additional Resources

- [Demo.JsonApi README](README.md)
- [QuickApiMapper Documentation](../../README.md)
- [Scalar API Docs](https://localhost:7100/scalar/v1)
- [Aspire Dashboard](http://localhost:15000) (when running via AppHost)

## Support

For issues or questions:
- Check the main [QuickApiMapper repository](https://github.com/jerrettdavis/QuickApiMapper)
- Review example integration configurations in the `Examples/` directory
- Examine the Scalar API documentation for detailed endpoint information
