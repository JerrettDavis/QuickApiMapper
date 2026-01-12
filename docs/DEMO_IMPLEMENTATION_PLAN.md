# QuickApiMapper Demo Implementation Plan

## Executive Summary

This plan outlines the implementation of a comprehensive, user-facing demo that showcases QuickApiMapper's ability to seamlessly transform data between modern JSON APIs and legacy SOAP services, with message queue integration and real-time dashboard tracking.

## Demo Scenario: E-Commerce Order Fulfillment Pipeline

### Business Context

A modern e-commerce platform (JSON API) needs to integrate with a legacy warehouse management system (SOAP API). Orders flow through multiple channels:
- Direct API calls from the e-commerce platform
- Message queue processing for batch operations
- Real-time tracking and monitoring through the dashboard

### Data Flow Architecture

```
┌─────────────────────┐
│  E-Commerce System  │
│    (JSON API)       │
└──────────┬──────────┘
           │ POST /api/orders
           ▼
┌─────────────────────────────────────────────────────────┐
│              QuickApiMapper Gateway                      │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Integration: json-to-soap-orders                 │  │
│  │  • Receives JSON order                            │  │
│  │  • Applies field mappings & transformers          │  │
│  │  • Converts to SOAP envelope                      │  │
│  │  • Captures input/output messages                 │  │
│  └──────────────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────────────┘
             │ SOAP Request
             ▼
    ┌────────────────────┐
    │ Warehouse System   │
    │   (SOAP API)       │
    └────────────────────┘

Alternative Flow via Message Queue:
┌──────────────┐
│  RabbitMQ    │ ──► QuickApiMapper Worker ──► Transformation ──► Destination
└──────────────┘

Monitoring:
All transformations ──► Message Capture ──► Designer Dashboard
```

## Implementation Components

### 1. Demo.JsonApi Project

**Purpose**: Simulates a modern e-commerce order API

**Technology Stack**:
- ASP.NET Core Minimal API
- In-memory data store
- OpenAPI/Swagger documentation

**Endpoints**:
- `POST /api/orders` - Submit new order (JSON)
- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get specific order
- `PUT /api/orders/{id}/status` - Update order status
- `GET /health` - Health check

**Sample JSON Order Model**:
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
  "priority": "STANDARD"
}
```

### 2. Demo.SoapApi Project

**Purpose**: Simulates a legacy warehouse/ERP SOAP service

**Technology Stack**:
- ASP.NET Core with SOAP support
- WCF-style service contract
- WSDL generation

**SOAP Operations**:
- `SubmitFulfillmentRequest` - Receive order fulfillment (SOAP)
- `GetFulfillmentStatus` - Query fulfillment status
- `CancelFulfillment` - Cancel order

**Sample SOAP Request**:
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

### 3. QuickApiMapper Integration Configurations

**Integration 1: JSON to SOAP Order Processing**

- **Name**: `json-to-soap-orders`
- **Endpoint**: `/api/fulfillment/submit`
- **Source Type**: JSON
- **Destination Type**: SOAP
- **Destination URL**: `http://demo-soapapi/WarehouseService.asmx`

**Field Mappings**:
| Source (JSONPath) | Destination (XPath) | Transformers |
|-------------------|---------------------|--------------|
| `$.orderId` | `/OrderNumber` | - |
| `$.customerName` | `/CustomerInfo/Name` | - |
| `$.customerEmail` | `/CustomerInfo/ContactEmail` | ToLower |
| `$.orderDate` | `/OrderDateTime` | - |
| `$.totalAmount` | `/TotalValue` | - |
| `$.currency` | `/CurrencyCode` | - |
| `$.items[*].sku` | `/LineItems/Item/SKU` | ToUpper |
| `$.items[*].productName` | `/LineItems/Item/Description` | - |
| `$.items[*].quantity` | `/LineItems/Item/Qty` | - |
| `$.items[*].unitPrice` | `/LineItems/Item/Price` | - |
| `$.shippingAddress.street` | `/DeliveryAddress/AddressLine1` | - |
| `$.shippingAddress.city` | `/DeliveryAddress/City` | - |
| `$.shippingAddress.state` | `/DeliveryAddress/StateProvince` | - |
| `$.shippingAddress.postalCode` | `/DeliveryAddress/PostalCode` | - |
| `$.shippingAddress.country` | `/DeliveryAddress/CountryCode` | - |
| `$.priority` | `/PriorityCode` | CustomPriorityMapper |

**SOAP Configuration**:
- Service URL: `http://demo-soapapi/WarehouseService.asmx`
- Action: `http://warehouse.example.com/SubmitFulfillmentRequest`
- Namespace: `http://warehouse.example.com/`
- Root Element: `SubmitFulfillmentRequest`

**Integration 2: SOAP to JSON Fulfillment Status**

- **Name**: `soap-to-json-status`
- **Endpoint**: `/api/fulfillment/status`
- **Source Type**: SOAP
- **Destination Type**: JSON
- **Destination URL**: `http://demo-jsonapi/api/orders/{orderId}/status`

### 4. Custom Transformers for Demo

**PriorityMapper Transformer**:
- `STANDARD` → `STD`
- `EXPRESS` → `EXP`
- `OVERNIGHT` → `OVN`

**PhoneFormatter Transformer**:
- Format phone numbers to E.164 standard

**CurrencyConverter Transformer**:
- Demonstrate currency conversion (with static rates for demo)

### 5. RabbitMQ Worker Enhancement

**Current State**: Worker consumes messages but has TODO for processing

**Enhancement**:
1. Integrate with `IMappingEngineFactory`
2. Route messages through configured integrations
3. Support both JSON and SOAP payloads
4. Implement message acknowledgment after successful processing
5. Dead-letter failed transformations
6. Enable message capture for worker-processed messages

**Message Flow**:
```
RabbitMQ Queue → Worker.ProcessMessageAsync()
              → Determine Integration (by routing key/message type)
              → Execute Mapping Engine
              → Forward to Destination
              → Acknowledge Message
              → Capture in History
```

### 6. Demo Seed Data

**Pre-configured Integrations**:
1. `json-to-soap-orders` (as described above)
2. `soap-to-json-status`
3. `rabbitmq-order-batch` (for worker demo)

**Sample Orders**:
- 10 pre-seeded orders showing various scenarios:
  - Standard priority
  - Express shipping
  - International orders
  - Multi-item orders
  - Edge cases (special characters, long addresses)

**Message Capture History**:
- Pre-populate 20-30 successful transformations
- Include 2-3 failed scenarios for error handling demo

### 7. Aspire AppHost Configuration Updates

**New Service Additions**:
```csharp
// Demo JSON API
var demoJsonApi = builder.AddProject<Projects.Demo_JsonApi>("demo-jsonapi")
    .WithExternalHttpEndpoints();

// Demo SOAP API
var demoSoapApi = builder.AddProject<Projects.Demo_SoapApi>("demo-soapapi")
    .WithExternalHttpEndpoints();

// Update Web API to reference demo services
var webApi = builder.AddProject<Projects.QuickApiMapper_Web>("web")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(demoJsonApi)
    .WithReference(demoSoapApi)
    .WithExternalHttpEndpoints();
```

**Service Discovery**:
- Enable service-to-service communication via Aspire service discovery
- Configure health checks for all services

### 8. Designer Dashboard Enhancements

**New Dashboard Components**:

1. **Demo Mode Landing Page** (`DemoWalkthrough.razor`)
   - Overview of the demo scenario
   - Step-by-step guide
   - Quick-start buttons

2. **Live Message Flow Visualization** (`MessageFlowDiagram.razor`)
   - Real-time visualization using MudBlazor components
   - Show transformation pipeline
   - Highlight active transformations

3. **Demo Statistics Dashboard** (`DemoStats.razor`)
   - Total transformations processed
   - Success rate by integration
   - Average processing time
   - Message volume over time (chart)

4. **Interactive Demo Runner** (`DemoRunner.razor`)
   - Pre-configured sample requests
   - One-click submission
   - Real-time result display
   - Message capture correlation

**Enhanced Message History**:
- Filter by demo/non-demo messages
- Diff view (input vs output)
- JSON/SOAP formatting with syntax highlighting
- Timeline view

### 9. Demo Documentation & Guides

**Documents to Create**:

1. **DEMO_GUIDE.md**
   - Complete walkthrough of the demo
   - Prerequisites and setup
   - Step-by-step execution
   - Expected results
   - Troubleshooting

2. **API_SAMPLES.md**
   - Sample cURL commands
   - Postman collection export
   - Sample requests/responses
   - Integration testing examples

3. **ARCHITECTURE_DIAGRAM.md**
   - Visual architecture using Mermaid diagrams
   - Data flow illustrations
   - Component interactions

4. **VIDEO_SCRIPT.md**
   - Script for demo video/presentation
   - Key talking points
   - Screenshots to capture

## Implementation Phases

### Phase 1: Foundation (Agents 1-3)
- **Agent 1**: Create Demo.JsonApi project with sample endpoints
- **Agent 2**: Create Demo.SoapApi project with WSDL service
- **Agent 3**: Create custom transformers for demo scenarios

### Phase 2: Integration (Agents 4-5)
- **Agent 4**: Enhance RabbitMQ worker with mapping engine integration
- **Agent 5**: Create demo seed data and database migrations

### Phase 3: Orchestration (Agent 6)
- **Agent 6**: Update Aspire AppHost with demo services and dependencies

### Phase 4: UI/UX (Agents 7-8)
- **Agent 7**: Create demo dashboard components and visualizations
- **Agent 8**: Enhance message history with demo features

### Phase 5: Documentation (Agent 9)
- **Agent 9**: Create comprehensive demo documentation and guides

### Phase 6: Testing & Polish (Agent 10)
- **Agent 10**: End-to-end testing, bug fixes, and polish

## Success Criteria

The demo will be considered successful when:

1. ✅ User can submit a JSON order via Demo.JsonApi
2. ✅ QuickApiMapper automatically transforms JSON → SOAP
3. ✅ SOAP request is received by Demo.SoapApi
4. ✅ Both input and output messages are captured
5. ✅ User can view transformation in Designer dashboard
6. ✅ RabbitMQ worker processes messages through the pipeline
7. ✅ Statistics and metrics are displayed accurately
8. ✅ Demo can run end-to-end with one Aspire `dotnet run` command
9. ✅ Documentation is clear and comprehensive
10. ✅ Demo showcases all key QuickApiMapper features:
    - JSON ↔ SOAP transformation
    - Field mapping with transformers
    - Message queue integration
    - Real-time monitoring
    - Configuration management

## Technical Considerations

### Performance
- Demo services should respond within 100ms
- Support at least 100 concurrent transformations
- Message capture should not impact throughput

### Error Handling
- Graceful degradation if demo services are unavailable
- Clear error messages in dashboard
- Failed messages captured for debugging

### Data Persistence
- In-memory storage for demo services (reset on restart)
- Persistent message capture in PostgreSQL
- Demo mode flag to separate demo data

### Security
- Demo endpoints don't require authentication (marked as demo)
- Production mode disables demo services
- Sample data doesn't contain sensitive information

## Future Enhancements

After initial demo implementation:
- Add webhook notifications
- Implement retry logic visualization
- Add GraphQL demo endpoint
- Create interactive tutorial mode
- Record demo video
- Create public demo deployment

## Timeline Estimate

- Phase 1: ~2-3 hours
- Phase 2: ~2-3 hours
- Phase 3: ~1 hour
- Phase 4: ~2-3 hours
- Phase 5: ~1-2 hours
- Phase 6: ~1-2 hours

**Total**: ~9-14 hours of development time

## Resources Required

- .NET 10 SDK
- Docker Desktop (for Aspire)
- Visual Studio 2022 / Rider / VS Code
- Postman (for testing)
- Database tools (pgAdmin)

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| SOAP service complexity | Use SoapCore library, well-documented |
| Worker integration issues | Thorough testing, fallback to manual trigger |
| Performance degradation | Load testing, optimize message capture |
| Aspire configuration issues | Incremental testing, clear error messages |
| Demo data conflicts | Namespaced demo integrations, clear markers |

---

**Document Version**: 1.0
**Last Updated**: 2026-01-10
**Status**: Ready for Implementation
