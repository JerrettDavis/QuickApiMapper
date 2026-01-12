# QuickApiMapper Aspire Architecture

## Service Orchestration Overview

This document provides a comprehensive view of how all QuickApiMapper services are orchestrated via .NET Aspire.

## Service Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                          │
│  (Start first, in parallel)                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │  PostgreSQL  │    │    Redis     │    │   RabbitMQ   │      │
│  │              │    │              │    │              │      │
│  │  + PgAdmin   │    │              │    │  + Mgmt UI   │      │
│  └──────────────┘    └──────────────┘    └──────────────┘      │
│         │                   │                    │               │
└─────────┼───────────────────┼────────────────────┼───────────────┘
          │                   │                    │
          │                   │                    │
          ▼                   ▼                    │
┌─────────────────────────────────────────────────┼───────────────┐
│                  MANAGEMENT LAYER                │               │
│  (After infrastructure)                          │               │
├──────────────────────────────────────────────────┼───────────────┤
│                                                  │               │
│  ┌────────────────────────────────────┐         │               │
│  │      Management API                │         │               │
│  │                                     │         │               │
│  │  • Integration mappings            │         │               │
│  │  • Demo data seeding (Dev)         │         │               │
│  │  • Configuration management        │         │               │
│  └────────────────────────────────────┘         │               │
│                      │                           │               │
│                      │ (dependency)              │               │
└──────────────────────┼───────────────────────────┼───────────────┘
                       │                           │
                       ▼                           │
┌─────────────────────────────────────────────────┼───────────────┐
│               APPLICATION LAYER                  │               │
│  (After Management API)                          │               │
├──────────────────────────────────────────────────┼───────────────┤
│                                                  │               │
│  ┌────────────────────────────────────┐         │               │
│  │       Designer Web UI              │         │               │
│  │                                     │         │               │
│  │  • Visual mapping creator          │         │               │
│  │  • Blazor Server UI                │         │               │
│  └────────────────────────────────────┘         │               │
│                                                  │               │
│  ┌────────────────────────────────────┐         │               │
│  │      QuickApiMapper Web API        │◄────────┘               │
│  │                                     │                         │
│  │  • Integration execution           │                         │
│  │  • JSON/SOAP transformations       │                         │
│  │  • Message routing                 │                         │
│  └────────────────────────────────────┘                         │
│              │               │                                   │
│              │               │ (service discovery)               │
└──────────────┼───────────────┼───────────────────────────────────┘
               │               │
               ▼               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     DEMO SERVICES LAYER                          │
│  (Standalone, no dependencies)                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────┐         ┌────────────────────┐          │
│  │  Demo JSON API     │         │  Demo SOAP API     │          │
│  │                    │         │                    │          │
│  │  • Order API       │         │  • Warehouse API   │          │
│  │  • REST/JSON       │         │  • SOAP 1.1/1.2    │          │
│  │  • Modern system   │         │  • Legacy system   │          │
│  └────────────────────┘         └────────────────────┘          │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow in Demo Mode

```
┌──────────────┐
│   Client     │
│   Request    │
└──────┬───────┘
       │
       │ 1. Submit order (JSON)
       ▼
┌────────────────────────────────────┐
│      Demo JSON API                 │
│  POST /api/orders                  │
│  {                                 │
│    "orderId": "ORD-001",          │
│    "customer": {...},              │
│    "items": [...]                  │
│  }                                 │
└────────────────┬───────────────────┘
                 │
                 │ 2. Forward to QuickApiMapper
                 ▼
┌────────────────────────────────────┐
│   QuickApiMapper Web API           │
│                                    │
│  • Load mapping config from        │
│    Management API                  │
│  • Transform JSON → SOAP           │
│  • Apply field mappings            │
│  • Generate SOAP envelope          │
└────────────────┬───────────────────┘
                 │
                 │ 3. SOAP request
                 ▼
┌────────────────────────────────────┐
│      Demo SOAP API                 │
│  SubmitFulfillmentRequest          │
│  <soap:Envelope>                   │
│    <OrderNumber>ORD-001</...>     │
│    <Items>...</Items>              │
│  </soap:Envelope>                  │
└────────────────┬───────────────────┘
                 │
                 │ 4. SOAP response
                 ▼
┌────────────────────────────────────┐
│   QuickApiMapper Web API           │
│                                    │
│  • Parse SOAP response             │
│  • Transform SOAP → JSON           │
│  • Apply response mappings         │
│  • Format JSON response            │
└────────────────┬───────────────────┘
                 │
                 │ 5. JSON response
                 ▼
┌────────────────────────────────────┐
│         Client                     │
│  {                                 │
│    "confirmationNumber": "FUL-123",│
│    "status": "Pending",            │
│    "estimatedShipDate": "..."      │
│  }                                 │
└────────────────────────────────────┘
```

## Service Configuration Details

### Demo JSON API
```yaml
Name: demo-jsonapi
Type: ASP.NET Core Web API
Protocol: HTTP/HTTPS
Dependencies: None
Health Check: /health
External Access: Yes
Service Discovery: Enabled

Endpoints:
  - POST /api/orders
  - GET /api/orders
  - GET /api/orders/{id}
  - PUT /api/orders/{id}/status

Features:
  - OpenAPI/Swagger via Scalar
  - In-memory storage
  - Health checks via Aspire
```

### Demo SOAP API
```yaml
Name: demo-soapapi
Type: ASP.NET Core + SoapCore
Protocol: SOAP 1.1/1.2
Dependencies: None
Health Check: /health
External Access: Yes
Service Discovery: Enabled

Endpoints:
  - /WarehouseService.asmx (SOAP)
  - /WarehouseService.asmx?wsdl (WSDL)
  - / (Information page)

Operations:
  - SubmitFulfillmentRequest
  - GetFulfillmentStatus
  - CancelFulfillment

Features:
  - In-memory storage
  - Health checks via Aspire
  - HTML documentation page
```

### Management API
```yaml
Name: management-api
Type: ASP.NET Core Web API
Dependencies:
  - PostgreSQL (database)
  - Redis (caching)
Health Check: /health
External Access: Yes

Configuration (Development):
  Environment Variables:
    - DemoMode__EnableDemoMode: true
    - DemoMode__ForceReseed: false
    - DemoMode__SampleMessageCount: 15
    - DemoMode__FailedMessageCount: 3

Features:
  - Integration mapping CRUD
  - Demo data seeding
  - Message history
  - RESTful API
```

### Web API
```yaml
Name: web-api
Type: ASP.NET Core Web API
Dependencies:
  - PostgreSQL (persistence)
  - Redis (caching)
  - RabbitMQ (messaging)
  - Management API (config)
  - Demo JSON API (service discovery)
  - Demo SOAP API (service discovery)
Health Check: /health
External Access: Yes

Features:
  - Integration mapping execution
  - JSON/SOAP transformations
  - Message capture
  - Service discovery for demo APIs
```

### Designer Web
```yaml
Name: designer-web
Type: Blazor Server
Dependencies:
  - Management API (data/config)
Health Check: /health
External Access: Yes

Features:
  - Visual mapping designer
  - Integration testing UI
  - Message history viewer
```

## Infrastructure Services

### PostgreSQL
```yaml
Name: postgres
Database: quickapimapper-db
UI: PgAdmin
Used By:
  - Management API
  - Web API

Configuration:
  - Auto-created by Aspire
  - Persistent storage
  - Connection string injected
```

### Redis
```yaml
Name: redis
Used By:
  - Management API
  - Web API

Configuration:
  - Auto-created by Aspire
  - Connection string injected
```

### RabbitMQ
```yaml
Name: rabbitmq
UI: Management Plugin
Used By:
  - Web API (message queuing)

Configuration:
  - Auto-created by Aspire
  - Management UI enabled
  - Connection string injected
```

## Startup Sequence

### Phase 1: Infrastructure (Parallel)
```
t=0s: PostgreSQL starts
t=0s: Redis starts
t=0s: RabbitMQ starts
t=2s: Infrastructure ready
```

### Phase 2: Demo Services (Parallel)
```
t=0s: Demo JSON API starts
t=0s: Demo SOAP API starts
t=1s: Demo services ready
```

### Phase 3: Management Layer
```
t=2s: Management API starts
       ↓ Waits for PostgreSQL
       ↓ Waits for Redis
t=3s: Management API ready
       ↓ Seeds demo data (Development only)
t=4s: Demo data seeded
```

### Phase 4: Application Layer
```
t=4s: Web API starts
       ↓ Waits for Management API
       ↓ Waits for Demo JSON API
       ↓ Waits for Demo SOAP API
t=5s: Web API ready

t=4s: Designer Web starts
       ↓ Waits for Management API
t=5s: Designer Web ready
```

### Total Startup Time
```
~5-7 seconds (all services running)
```

## Service Discovery Mechanism

### How Services Find Each Other

1. **Infrastructure Services** (PostgreSQL, Redis, RabbitMQ):
   - Injected as connection strings via `.WithReference()`
   - Format: `ConnectionStrings__<servicename>`
   - Example: `ConnectionStrings__postgres=Host=postgres;...`

2. **Application Services** (Demo APIs):
   - Registered with service discovery via `.WithReference()`
   - Resolved via HttpClient with service names
   - Example: `httpClient.GetAsync("http://demo-jsonapi/api/orders")`

3. **Service Defaults**:
   - All services use `QuickApiMapper.Host.ServiceDefaults`
   - Provides automatic service discovery configuration
   - Adds resilience handlers to HttpClient

### Example: Web API Calling Demo SOAP API

```csharp
// In Web API code
var httpClient = httpClientFactory.CreateClient();

// Service discovery resolves "demo-soapapi" to actual URL
var response = await httpClient.PostAsync(
    "http://demo-soapapi/WarehouseService.asmx",
    soapContent);
```

## Health Check Strategy

### Health Check Endpoints

All services expose:
- `/health` - Overall health (used by Aspire)
- `/alive` - Liveness probe (service is running)

### Health Check Configuration

```csharp
// In AppHost.cs
.WithHttpHealthCheck("/health")

// In service Program.cs (via ServiceDefaults)
builder.AddServiceDefaults();  // Adds health checks
app.MapDefaultEndpoints();      // Exposes /health and /alive
```

### Monitoring

- Aspire Dashboard shows real-time health status
- Green: Healthy
- Yellow: Degraded
- Red: Unhealthy

## Environment-Specific Configuration

### Development Environment

```csharp
if (builder.Environment.EnvironmentName == "Development")
{
    // Enable demo mode
    managementApi.WithEnvironment("DemoMode__EnableDemoMode", "true");

    // Additional development settings
    // - Verbose logging
    // - External endpoints for all services
    // - PgAdmin UI
    // - RabbitMQ Management UI
}
```

### Production Environment

```csharp
// Demo mode disabled by default
// - Reduced logging
// - Secure endpoints
// - No management UIs
// - Proper secrets management
```

## Benefits of This Architecture

### 1. Complete Orchestration
- Single command starts entire stack
- Automatic dependency management
- Proper startup ordering

### 2. Service Discovery
- No hardcoded URLs
- Dynamic service resolution
- Environment-independent

### 3. Observability
- Centralized logging
- Distributed tracing
- Performance metrics
- Real-time monitoring

### 4. Development Experience
- Fast inner loop
- Easy debugging
- Integrated tooling
- Visual service management

### 5. Demo Capability
- Automatic demo data seeding
- Complete integration example
- Self-contained testing

## Troubleshooting Guide

### Service Won't Start

1. Check dependencies are healthy
2. Review logs in Aspire Dashboard
3. Verify port availability
4. Check resource limits (Docker)

### Database Issues

1. Verify PostgreSQL is running
2. Check connection string
3. Review migrations status
4. Inspect PgAdmin

### Service Discovery Issues

1. Verify service names match
2. Check `.WithReference()` calls
3. Review HttpClient configuration
4. Inspect network traces

### Demo Mode Not Working

1. Confirm Development environment
2. Check Management API env vars
3. Review seeding logs
4. Verify database state

## Summary

The Aspire AppHost provides:

1. **Complete orchestration** of all services
2. **Proper dependency management** with `.WaitFor()`
3. **Service discovery** for demo APIs
4. **Health monitoring** for all services
5. **Demo mode automation** in Development
6. **Observability** via Aspire Dashboard
7. **Single-command startup** for entire ecosystem

The architecture enables:
- **Rapid development** with fast iteration
- **Easy testing** with integrated demo services
- **Clear data flow** from JSON to SOAP
- **Production readiness** with environment-specific config
