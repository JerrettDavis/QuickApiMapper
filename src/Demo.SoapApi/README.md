# Demo.SoapApi - Legacy Warehouse/ERP SOAP Service

A demonstration ASP.NET Core SOAP web service that simulates a legacy enterprise warehouse management system. This service is designed to work with QuickApiMapper for JSON-to-SOAP integration scenarios.

## Overview

This project provides a realistic example of a legacy SOAP-based enterprise system that QuickApiMapper can integrate with. It implements a typical warehouse/ERP fulfillment workflow with three core operations:

1. **SubmitFulfillmentRequest** - Submit new orders for fulfillment
2. **GetFulfillmentStatus** - Query order status
3. **CancelFulfillment** - Cancel pending orders

## Technology Stack

- **.NET 10** - Modern ASP.NET Core runtime
- **SoapCore 1.2.1.12** - SOAP endpoint implementation
- **Aspire Service Defaults** - Health checks, telemetry, and service discovery
- **In-Memory Storage** - Simple storage for demo purposes

## SOAP Service Details

### Namespace
```
http://warehouse.example.com/
```

### Endpoint
```
/WarehouseService.asmx
```

### WSDL
```
/WarehouseService.asmx?wsdl
```

## Sample SOAP Request

### SubmitFulfillmentRequest

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <SubmitFulfillmentRequest xmlns="http://warehouse.example.com/">
      <OrderNumber>ORD-2026-001</OrderNumber>
      <CustomerInfo>
        <Name>John Smith</Name>
        <ContactEmail>john.smith@example.com</ContactEmail>
      </CustomerInfo>
      <OrderDateTime>2026-01-10T14:30:00</OrderDateTime>
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

### Sample Response

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <SubmitFulfillmentResponse xmlns="http://warehouse.example.com/">
      <Success>true</Success>
      <ConfirmationNumber>WH-20260110-A1B2C3D4</ConfirmationNumber>
      <OrderNumber>ORD-2026-001</OrderNumber>
      <ProcessedDateTime>2026-01-10T14:30:05</ProcessedDateTime>
      <Status>PENDING</Status>
      <Message>Fulfillment request accepted and queued for processing</Message>
      <EstimatedShipDate>2026-01-13T14:30:05</EstimatedShipDate>
    </SubmitFulfillmentResponse>
  </soap:Body>
</soap:Envelope>
```

## Priority Codes

- **EXP** or **EXPRESS** - 1-day processing, 1-day delivery
- **PRI** or **PRIORITY** - 2-day processing, 2-day delivery
- **STD** or **STANDARD** - 3-day processing (default), 3-day delivery

## Status Progression

The service simulates realistic order progression:

1. **PENDING** - Initial state when order is submitted
2. **PROCESSING** - Warehouse is preparing the order
3. **SHIPPED** - Order has been shipped (tracking number assigned)
4. **DELIVERED** - Order has been delivered to customer
5. **CANCELLED** - Order was cancelled before delivery

Status automatically progresses based on time elapsed since submission and priority code.

## Running the Service

### Standalone
```bash
cd src/Demo.SoapApi
dotnet run
```

The service will be available at:
- HTTP: http://localhost:5100
- HTTPS: https://localhost:7100

### With Aspire AppHost
```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

The SOAP API will be orchestrated alongside other QuickApiMapper services.

## Integration with QuickApiMapper

This SOAP service is designed to be integrated with QuickApiMapper:

1. **Configure an Integration** in QuickApiMapper Management API
2. **Set Source Type** to JSON (modern REST API)
3. **Set Destination Type** to SOAP
4. **Configure Field Mappings** from JSON paths to SOAP XML paths
5. **Set Destination URL** to the SOAP endpoint

Example mapping configuration:
```json
{
  "name": "E-Commerce Order Fulfillment",
  "sourceType": "JSON",
  "destinationType": "SOAP",
  "destinationUrl": "https://localhost:7100/WarehouseService.asmx",
  "fieldMappings": [
    {
      "source": "$.orderId",
      "destination": "/envelope/body/OrderNumber",
      "order": 1
    },
    {
      "source": "$.customer.name",
      "destination": "/envelope/body/CustomerInfo/Name",
      "order": 2
    }
    // ... additional mappings
  ]
}
```

## Architecture

### Models
- **FulfillmentModels.cs** - SOAP contract models with DataContract attributes
- **FulfillmentRecord.cs** - Internal storage model

### Services
- **IWarehouseService.cs** - SOAP service contract interface
- **WarehouseService.cs** - SOAP service implementation with business logic

### Storage
- **IFulfillmentRepository.cs** - Repository abstraction
- **InMemoryFulfillmentRepository.cs** - In-memory implementation (thread-safe)

## Testing with SOAP UI or Postman

You can test the SOAP service using:

1. Import the WSDL from `/WarehouseService.asmx?wsdl`
2. Generate request templates
3. Modify the sample data
4. Send requests to the endpoint

## Health Checks

The service includes Aspire health check endpoints:
- `/health` - Overall health status
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

## Logging

Logs are written to console with detailed information about:
- Incoming SOAP requests
- Order processing
- Status queries
- Cancellations
- Errors

## Notes

- This is a **demo/simulation** service for testing QuickApiMapper
- Uses **in-memory storage** - data is lost on restart
- Automatically simulates status progression based on time
- Generates realistic confirmation numbers and tracking numbers
- Validates required fields and business rules

## Future Enhancements

Potential additions for a production-ready version:
- Persistent database storage
- Authentication and authorization
- Rate limiting
- Message queuing for async processing
- External carrier API integration
- Inventory management
- Multi-warehouse support
