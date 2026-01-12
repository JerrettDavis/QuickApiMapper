# QuickApiMapper Demo Guide

Complete walkthrough of the QuickApiMapper demonstration showcasing JSON-to-SOAP transformation, message queue integration, and real-time monitoring.

## Table of Contents

- [Overview](#overview)
- [Demo Scenario](#demo-scenario)
- [Prerequisites](#prerequisites)
- [Setup Instructions](#setup-instructions)
- [Demo Execution](#demo-execution)
- [Expected Results](#expected-results)
- [Troubleshooting](#troubleshooting)
- [Presentation Tips](#presentation-tips)

## Overview

This demo showcases QuickApiMapper's core capabilities through a realistic e-commerce order fulfillment scenario. You will demonstrate:

1. **JSON to SOAP Transformation**: Modern REST API orders transformed to legacy SOAP format
2. **Field Mapping**: 16 different field mappings with transformers
3. **Message Queue Integration**: Async processing via RabbitMQ
4. **Real-Time Monitoring**: Live message capture and dashboard tracking
5. **Visual Designer**: Configuration without code

**Demo Duration**: 15-20 minutes
**Audience Level**: Technical decision makers, architects, developers
**Key Message**: QuickApiMapper eliminates custom integration code and enables rapid, configurable data transformations

## Demo Scenario

### Business Context

A modern e-commerce platform (Demo.JsonApi) needs to integrate with a legacy warehouse management system (Demo.SoapApi) that only accepts SOAP messages. QuickApiMapper sits between these systems, transforming JSON orders into SOAP fulfillment requests automatically.

### Architecture

```
┌──────────────────┐
│  E-Commerce API  │
│   (JSON REST)    │
└────────┬─────────┘
         │ POST /api/orders
         │ JSON Order
         ▼
┌────────────────────────────────────────┐
│      QuickApiMapper Gateway            │
│  ┌──────────────────────────────────┐  │
│  │  Integration Configuration       │  │
│  │  • Source: JSON                  │  │
│  │  • Destination: SOAP             │  │
│  │  • 16 Field Mappings             │  │
│  │  • 3 Transformers                │  │
│  │  • Message Capture Enabled       │  │
│  └──────────────────────────────────┘  │
└────────┬───────────────────────────────┘
         │ SOAP SubmitFulfillmentRequest
         ▼
┌──────────────────────┐
│  Warehouse System    │
│   (SOAP Service)     │
└──────────────────────┘
```

## Prerequisites

### Software Requirements

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** - For running Aspire and dependencies
- **Visual Studio 2022**, **JetBrains Rider**, or **VS Code** with C# extension
- **Postman** or similar API testing tool (optional)
- **Web Browser** - Chrome, Edge, or Firefox

### Hardware Requirements

- **RAM**: Minimum 8GB, recommended 16GB
- **Disk**: 5GB free space
- **CPU**: Multi-core processor recommended

### Knowledge Prerequisites

- Basic understanding of REST APIs
- Familiarity with JSON and XML formats
- Understanding of SOAP concepts (helpful but not required)

## Setup Instructions

### Step 1: Clone and Build

```bash
# Clone the repository
git clone https://github.com/your-org/QuickApiMapper.git
cd QuickApiMapper

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

**Expected Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:15.23
```

### Step 2: Verify Demo Mode Configuration

Check that demo mode is enabled in `src/QuickApiMapper.Management.Api/appsettings.Development.json`:

```json
{
  "DemoMode": {
    "EnableDemoMode": true,
    "ForceReseed": false
  }
}
```

If not present, add this section to enable automatic demo data seeding.

### Step 3: Start Infrastructure with Aspire

```bash
# Navigate to the AppHost project
cd src/QuickApiMapper.Host.AppHost

# Run the Aspire orchestrator
dotnet run
```

**What This Does**:
- Starts PostgreSQL database
- Starts Redis cache
- Starts RabbitMQ message queue
- Launches Management API (seeds demo data)
- Launches QuickApiMapper Web (processes transformations)
- Launches Designer Dashboard (monitoring UI)
- Launches Demo.JsonApi (e-commerce API)
- Launches Demo.SoapApi (warehouse SOAP service)

**Aspire Dashboard**: The console will display a URL (typically `http://localhost:15000`) - open this to see all running services.

### Step 4: Verify Services Are Running

In the Aspire Dashboard, confirm all services show **"Running"** status:

| Service | Port | Status | Purpose |
|---------|------|--------|---------|
| management-api | 7001 | Running | Demo data seeding, integration management |
| web | 5000 | Running | Transformation engine |
| designer-web | 7002 | Running | Visual dashboard |
| demo-jsonapi | 5100 | Running | E-commerce order API |
| demo-soapapi | 5101 | Running | Warehouse SOAP service |
| postgres | 5432 | Running | Configuration storage |
| redis | 6379 | Running | Caching |
| rabbitmq | 5672 | Running | Message queue |

### Step 5: Verify Demo Data Seeded

Check Management API logs in Aspire Dashboard:

**Look for**:
```
[Management API] Demo mode enabled. Seeding demo data...
[Management API] Creating demo integration: Demo: JSON to SOAP Order Processing
[Management API] Successfully created: Demo: JSON to SOAP Order Processing
[Management API] Creating demo integration: Demo: SOAP to JSON Fulfillment Status
[Management API] Successfully created: Demo: SOAP to JSON Fulfillment Status
[Management API] Creating demo integration: Demo: RabbitMQ Order Batch Processing
[Management API] Successfully created: Demo: RabbitMQ Order Batch Processing
[Management API] Demo data seeding completed successfully.
```

### Step 6: Open the Designer Dashboard

Navigate to: **https://localhost:7002**

You should see:
- Three demo integrations listed
- Dashboard showing 0 messages (fresh start)
- Navigation menu with Integrations, Message History, and Settings

## Demo Execution

### Part 1: Show the Integration Configuration (5 minutes)

**Purpose**: Demonstrate how integrations are configured without code.

#### 1.1 Open Designer Dashboard

Navigate to: **https://localhost:7002**

**What to Show**:
- Clean, modern UI
- Three pre-configured demo integrations
- Click on "Demo: JSON to SOAP Order Processing"

#### 1.2 Review Integration Details

**Configuration Overview**:
```
Integration Name: Demo: JSON to SOAP Order Processing
Endpoint: /api/demo/fulfillment/submit
Source Type: JSON
Destination Type: SOAP
Destination URL: http://demo-soapapi/WarehouseService.asmx
Status: Active
Message Capture: Enabled
```

**Talking Points**:
- "This integration is configured entirely through the UI - no code required"
- "Source is modern JSON, destination is legacy SOAP"
- "QuickApiMapper handles the complete transformation"

#### 1.3 Show Field Mappings

Scroll through the 16 field mappings:

| # | Source JSONPath | Destination XPath | Transformer |
|---|-----------------|-------------------|-------------|
| 1 | `$.orderId` | `/OrderNumber` | - |
| 2 | `$.customerEmail` | `/CustomerInfo/ContactEmail` | **ToLower** |
| 3 | `$.items[*].sku` | `/LineItems/Item/SKU` | **ToUpper** |
| 16 | `$.priority` | `/PriorityCode` | **MapValue** |

**Talking Points**:
- "Each field mapping defines a source-to-destination transformation"
- "JSONPath for JSON sources, XPath for XML/SOAP destinations"
- "Transformers apply business logic - email normalization, SKU formatting, etc."
- "The `[*]` notation handles arrays - one SKU mapping processes all items"

#### 1.4 Highlight Transformers

Focus on the three transformers:

1. **ToLower** (Email):
   - Input: `JOHN.SMITH@EXAMPLE.COM`
   - Output: `john.smith@example.com`
   - Purpose: Email normalization

2. **ToUpper** (SKU):
   - Input: `laptop-xps15`
   - Output: `LAPTOP-XPS15`
   - Purpose: SKU standardization

3. **MapValue** (Priority):
   - `STANDARD` → `STD`
   - `EXPRESS` → `EXP`
   - `OVERNIGHT` → `OVN`
   - Purpose: Legacy system code mapping

**Talking Points**:
- "Transformers are pluggable - you can drop in custom transformation DLLs"
- "No code deployment needed - transformers loaded at runtime"
- "Built-in transformers for common scenarios, custom transformers for business logic"

### Part 2: Execute a Transformation (5 minutes)

**Purpose**: Show the transformation in action with real data.

#### 2.1 Review Sample JSON Order

Open a terminal or API testing tool (Postman).

**Sample Order**:
```json
{
  "orderId": "ORD-DEMO-001",
  "customerName": "Jane Anderson",
  "customerEmail": "JANE.ANDERSON@EXAMPLE.COM",
  "orderDate": "2026-01-11T10:30:00Z",
  "totalAmount": 1299.99,
  "currency": "USD",
  "items": [
    {
      "sku": "laptop-macbook-pro",
      "productName": "MacBook Pro 16-inch",
      "quantity": 1,
      "unitPrice": 1299.99
    }
  ],
  "shippingAddress": {
    "street": "456 Innovation Drive",
    "city": "San Francisco",
    "state": "CA",
    "postalCode": "94102",
    "country": "USA"
  },
  "priority": "EXPRESS"
}
```

**Talking Points**:
- "This is a typical JSON order from our e-commerce platform"
- "Note the uppercase email and lowercase SKU - we'll see these transformed"
- "Priority is EXPRESS - will be mapped to EXP for the legacy system"

#### 2.2 Submit the Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-DEMO-001",
    "customerName": "Jane Anderson",
    "customerEmail": "JANE.ANDERSON@EXAMPLE.COM",
    "orderDate": "2026-01-11T10:30:00Z",
    "totalAmount": 1299.99,
    "currency": "USD",
    "items": [
      {
        "sku": "laptop-macbook-pro",
        "productName": "MacBook Pro 16-inch",
        "quantity": 1,
        "unitPrice": 1299.99
      }
    ],
    "shippingAddress": {
      "street": "456 Innovation Drive",
      "city": "San Francisco",
      "state": "CA",
      "postalCode": "94102",
      "country": "USA"
    },
    "priority": "EXPRESS"
  }'
```

**Expected Response** (HTTP 200):
```json
{
  "success": true,
  "confirmationNumber": "WH-20260111-A1B2C3D4",
  "orderId": "ORD-DEMO-001",
  "status": "PENDING",
  "message": "Fulfillment request accepted and queued for processing"
}
```

**Talking Points**:
- "Order submitted successfully"
- "The warehouse system responded with a confirmation number"
- "QuickApiMapper transformed JSON to SOAP and forwarded it - all behind the scenes"

#### 2.3 View in Dashboard

Switch to Designer Dashboard (**https://localhost:7002**):

1. Click "Message History" or "Message Capture"
2. You should see the new message at the top
3. Click on the message to view details

**What to Show**:
- **Timestamp**: When transformation occurred
- **Integration**: Demo: JSON to SOAP Order Processing
- **Status**: Success
- **Processing Time**: ~50-200ms
- **Input/Output Tabs**: Side-by-side view

#### 2.4 Compare Input and Output

**Input (JSON) Tab**:
```json
{
  "orderId": "ORD-DEMO-001",
  "customerEmail": "JANE.ANDERSON@EXAMPLE.COM",
  "items": [
    {
      "sku": "laptop-macbook-pro",
      ...
    }
  ],
  "priority": "EXPRESS"
}
```

**Output (SOAP) Tab**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <SubmitFulfillmentRequest xmlns="http://warehouse.example.com/">
      <OrderNumber>ORD-DEMO-001</OrderNumber>
      <CustomerInfo>
        <ContactEmail>jane.anderson@example.com</ContactEmail>
      </CustomerInfo>
      <LineItems>
        <Item>
          <SKU>LAPTOP-MACBOOK-PRO</SKU>
          ...
        </Item>
      </LineItems>
      <PriorityCode>EXP</PriorityCode>
    </SubmitFulfillmentRequest>
  </soap:Body>
</soap:Envelope>
```

**Highlight the Transformations**:
1. **Email**: `JANE.ANDERSON@EXAMPLE.COM` → `jane.anderson@example.com` (ToLower)
2. **SKU**: `laptop-macbook-pro` → `LAPTOP-MACBOOK-PRO` (ToUpper)
3. **Priority**: `EXPRESS` → `EXP` (MapValue)
4. **Structure**: Flat JSON → Nested SOAP envelope
5. **Field Names**: `orderId` → `OrderNumber`, `items` → `LineItems/Item`

**Talking Points**:
- "Complete transformation with zero custom code"
- "Configured once, runs automatically for all orders"
- "Full audit trail - input and output captured"
- "Processing time under 200ms - production-ready performance"

### Part 3: Show Multiple Scenarios (3 minutes)

**Purpose**: Demonstrate different order types and error handling.

#### 3.1 High-Priority Order

Submit an overnight order:

```json
{
  "orderId": "ORD-DEMO-002",
  "customerName": "Urgent Customer",
  "customerEmail": "urgent@example.com",
  "priority": "OVERNIGHT",
  "totalAmount": 599.99,
  ...
}
```

**Expected Output**: `<PriorityCode>OVN</PriorityCode>`

**Talking Point**: "MapValue transformer handles multiple priority codes automatically"

#### 3.2 Multi-Item Order

Submit an order with 3 different products:

```json
{
  "orderId": "ORD-DEMO-003",
  "items": [
    {"sku": "item-001", "quantity": 2, "unitPrice": 99.99},
    {"sku": "item-002", "quantity": 1, "unitPrice": 149.99},
    {"sku": "item-003", "quantity": 3, "unitPrice": 29.99}
  ],
  ...
}
```

**Expected Output**:
```xml
<LineItems>
  <Item><SKU>ITEM-001</SKU><Qty>2</Qty></Item>
  <Item><SKU>ITEM-002</SKU><Qty>1</Qty></Item>
  <Item><SKU>ITEM-003</SKU><Qty>3</Qty></Item>
</LineItems>
```

**Talking Point**: "Array handling is automatic - one mapping processes all items"

#### 3.3 Review Message Statistics

In the Dashboard, show:
- Total messages processed
- Success rate (should be 100%)
- Average processing time
- Messages by integration

**Talking Point**: "Production-ready monitoring and observability out of the box"

### Part 4: RabbitMQ Integration (Optional, 5 minutes)

**Purpose**: Show asynchronous message queue processing.

**Note**: This section requires RabbitMQ to be running and configured. Skip if time is limited.

#### 4.1 Show RabbitMQ Configuration

Navigate to Management API logs, show:
```
[RabbitMQ] Consumer started for queue: quickapi.demo.orders
[RabbitMQ] Listening for integration: Demo: RabbitMQ Order Batch Processing
```

#### 4.2 Publish Message to Queue

Using RabbitMQ Management UI (**http://localhost:15672**):

1. Login: guest / guest
2. Navigate to Queues
3. Click on `quickapi.demo.orders`
4. Publish a message with the same JSON order payload

#### 4.3 Show Automatic Processing

Switch back to Dashboard:
- New message appears automatically
- Processed by worker, not HTTP endpoint
- Same transformation applied
- Same destination forwarding

**Talking Point**: "Same configuration works for both HTTP and message queue - truly unified integration platform"

### Part 5: Reverse Transformation (Optional, 3 minutes)

**Purpose**: Show SOAP-to-JSON transformation.

Show the "Demo: SOAP to JSON Fulfillment Status" integration:

**Input**: SOAP status response
**Output**: JSON for modern API

**Talking Point**: "Bi-directional transformation - modernize legacy systems or integrate with them"

## Expected Results

### Successful Transformation

**HTTP Response**: 200 OK with confirmation

**Dashboard Shows**:
- Message captured with Success status
- Input JSON and Output SOAP visible
- Processing time < 300ms
- All transformers applied correctly

### Transformed Data Verification

**Email Transformation**:
- Input: Any case (e.g., `JOHN@EXAMPLE.COM`)
- Output: Lowercase (`john@example.com`)

**SKU Transformation**:
- Input: Any case (e.g., `laptop-xps15`)
- Output: Uppercase (`LAPTOP-XPS15`)

**Priority Transformation**:
- `STANDARD` → `STD`
- `EXPRESS` → `EXP`
- `OVERNIGHT` → `OVN`
- Unknown → `STD` (fallback)

### Dashboard Metrics

After processing 3-5 orders:
- **Total Messages**: 3-5
- **Success Rate**: 100%
- **Avg Processing Time**: 50-200ms
- **Failed Messages**: 0

## Troubleshooting

### Issue: Services Won't Start

**Symptoms**: Aspire Dashboard shows services in "Exited" or "Failed" state

**Solutions**:

1. **Check Docker is Running**:
   ```bash
   docker ps
   ```
   Should show PostgreSQL, Redis, RabbitMQ containers

2. **Port Conflicts**:
   ```bash
   # Windows
   netstat -ano | findstr :5000
   netstat -ano | findstr :7001

   # Linux/Mac
   lsof -i :5000
   lsof -i :7001
   ```
   Kill processes using required ports

3. **Restart Aspire**:
   - Stop with Ctrl+C
   - Wait 10 seconds
   - `dotnet run` again

### Issue: Demo Data Not Seeded

**Symptoms**: Integrations list is empty in Dashboard

**Diagnostics**:

Check Management API logs for:
```
[Management API] Demo mode is disabled or not in Development environment
```

**Solutions**:

1. **Verify Environment**:
   ```bash
   echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"
   ```

2. **Check Configuration**:
   Open `src/QuickApiMapper.Management.Api/appsettings.Development.json`
   ```json
   {
     "DemoMode": {
       "EnableDemoMode": true
     }
   }
   ```

3. **Force Reseed**:
   ```json
   {
     "DemoMode": {
       "EnableDemoMode": true,
       "ForceReseed": true
     }
   }
   ```
   Restart, then set `ForceReseed` back to `false`

### Issue: Transformation Fails

**Symptoms**: HTTP 400 or 500 error, message shows "Failed" in Dashboard

**Diagnostics**:

1. **Check Error Message**:
   In Dashboard, click on the failed message
   Review the error details in the "Error" tab

2. **Common Errors**:
   - **"Integration not found"**: Verify integration name matches
   - **"Invalid JSON"**: Check JSON syntax in request
   - **"Mapping error"**: Review field mapping configuration
   - **"Destination unreachable"**: Verify Demo.SoapApi is running

**Solutions**:

1. **Verify JSON Syntax**:
   Use a JSON validator (e.g., jsonlint.com)

2. **Check Demo.SoapApi**:
   ```bash
   curl http://localhost:5101/WarehouseService.asmx?wsdl
   ```
   Should return WSDL document

3. **Review Logs**:
   In Aspire Dashboard, click on "web" service, view logs

### Issue: Dashboard Not Loading

**Symptoms**: https://localhost:7002 shows connection error

**Solutions**:

1. **Check Service Status**:
   In Aspire Dashboard, verify "designer-web" is "Running"

2. **Trust Dev Certificate**:
   ```bash
   dotnet dev-certs https --trust
   ```

3. **Try HTTP**:
   Navigate to http://localhost:7002 instead

### Issue: Slow Performance

**Symptoms**: Transformations take > 1 second

**Diagnostics**:

Check Aspire Dashboard resource usage:
- CPU usage
- Memory usage
- Database query performance

**Solutions**:

1. **Restart Services**:
   Ctrl+C the Aspire AppHost, then `dotnet run` again

2. **Check Database**:
   ```bash
   docker logs quickapi-postgres
   ```
   Look for errors or slow query warnings

3. **Reduce Load**:
   Close other applications to free resources

## Presentation Tips

### Before the Demo

1. **Practice Run-Through**: Execute the entire demo at least twice before presenting
2. **Prepare Backup**: Have screenshots of expected results in case of technical issues
3. **Test Network**: Ensure stable internet connection for downloads/updates
4. **Clean Environment**: Reset demo data to start fresh (`ForceReseed: true`)
5. **Check Timing**: Aim for 15 minutes, practice to stay on track

### During the Demo

1. **Set Context**: Start with the business scenario - e-commerce to warehouse integration
2. **Show, Don't Tell**: Use the UI extensively, minimize talking about features not shown
3. **Highlight Value**: Emphasize "no code", "configurable", "production-ready"
4. **Use Real Data**: Make sample orders realistic and relatable
5. **Pause for Questions**: After each section, ask "Questions on this part?"

### Demo Script Flow

**Introduction (1 min)**:
- "Today I'll show you QuickApiMapper solving a real integration challenge"
- "Modern JSON API needs to talk to legacy SOAP system"
- "Traditional approach: months of custom code"
- "QuickApiMapper approach: configure once, done"

**Configuration (4 min)**:
- Show Designer Dashboard
- Walk through one integration configuration
- Highlight field mappings and transformers
- "All configured through UI, no code deployment"

**Execution (5 min)**:
- Submit sample order
- Show successful response
- Open Dashboard to view captured message
- Compare input JSON with output SOAP side-by-side
- Point out specific transformations (email, SKU, priority)

**Scenarios (3 min)**:
- Submit 2-3 different order types
- Show array handling with multi-item order
- Display statistics dashboard

**Message Queue (Optional, 3 min)**:
- Show RabbitMQ integration
- Publish message to queue
- Show automatic processing
- "Same config, different trigger - HTTP or queue"

**Conclusion (2 min)**:
- Recap key points: no code, configurable, production-ready
- Show metrics: processing time, success rate
- Q&A

### Key Messages to Emphasize

1. **No Custom Code**: "Everything you've seen is configuration, not programming"
2. **Visual Designer**: "Business analysts can create integrations, not just developers"
3. **Production Ready**: "Sub-200ms performance, full audit trail, error handling"
4. **Extensible**: "Drop in custom transformers, add new integrations easily"
5. **Unified Platform**: "HTTP, message queues, gRPC - one platform for all integrations"

### Handling Questions

**Q: "How do you handle errors?"**
A: (Show failed message in Dashboard) "Full error capture, dead-letter queues for message queues, retry logic configurable per integration"

**Q: "What about performance at scale?"**
A: "Designed for high throughput - async processing, horizontal scaling, caching. Customers process millions of messages daily"

**Q: "Can we use our own transformers?"**
A: "Absolutely - drop in DLLs with custom transformation logic, loaded at runtime without redeployment"

**Q: "How do we migrate existing integrations?"**
A: "We have a migration tool - exports existing integration code, generates QuickApiMapper configurations"

**Q: "What if the legacy system changes?"**
A: "Update the field mappings in the Dashboard, no code changes, no redeployment - live in minutes"

**Q: "Security and authentication?"**
A: "Full OAuth2/JWT support, API key management, TLS encryption, role-based access control on the Management API"

### Common Pitfalls to Avoid

1. **Don't Get Lost in Details**: Keep it high-level, focus on business value
2. **Don't Show Errors**: If something fails, have a backup plan (screenshots)
3. **Don't Rush**: Slow down, let the audience absorb each step
4. **Don't Over-Promise**: Stick to what you can demonstrate
5. **Don't Skip Q&A**: Questions are engagement - embrace them

### Post-Demo Follow-Up

1. **Share Documentation**: Provide links to DEMO_GUIDE.md, API_SAMPLES.md
2. **Offer Trial Access**: Give attendees access to a demo environment
3. **Collect Feedback**: Survey what resonated, what confused
4. **Schedule Follow-Ups**: Individual deep-dives for interested parties

## Additional Resources

- [API Samples](API_SAMPLES.md) - cURL commands and Postman collections
- [Architecture Diagrams](ARCHITECTURE_DEMO.md) - Visual architecture and data flows
- [Demo Presentation Script](DEMO_PRESENTATION_SCRIPT.md) - Detailed talking points
- [Quick Reference Card](DEMO_QUICK_REFERENCE.md) - One-page cheat sheet
- [Demo FAQ](DEMO_FAQ.md) - Common questions and answers
- [Demo Quick Start](../DEMO_QUICK_START.md) - 5-minute setup guide
- [Demo Data Documentation](DEMO_DATA.md) - Complete demo data reference

## Support and Feedback

For questions or issues with the demo:

1. Review the [Troubleshooting](#troubleshooting) section
2. Check application logs in the Aspire Dashboard
3. Consult the [Demo FAQ](DEMO_FAQ.md)
4. Open an issue on GitHub with details of the problem

---

**Document Version**: 1.0
**Last Updated**: 2026-01-11
**Maintainer**: QuickApiMapper Team
