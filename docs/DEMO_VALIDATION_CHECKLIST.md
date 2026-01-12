# QuickApiMapper Demo - Validation Checklist

**Version:** 1.0
**Last Updated:** 2026-01-11
**Purpose:** Manual testing checklist for QuickApiMapper demo functionality

---

## Pre-Flight Checklist

Complete these steps **before** starting the demo:

### Build and Compilation

- [ ] **Solution builds successfully**
  ```bash
  cd /path/to/QuickApiMapper
  dotnet build
  # Expected: Build succeeded. 0 Warning(s). 0 Error(s).
  ```

- [ ] **All tests pass**
  ```bash
  dotnet test
  # Expected: All tests pass
  ```

- [ ] **Demo.SoapApi added to solution**
  ```bash
  dotnet sln list | grep Demo.SoapApi
  # Expected: src\Demo.SoapApi\Demo.SoapApi.csproj
  ```

### Infrastructure Dependencies

- [ ] **Docker installed and running** (for PostgreSQL, RabbitMQ, Redis)
  ```bash
  docker --version
  docker ps
  # Expected: Docker version 20.x or higher, running
  ```

- [ ] **.NET 10 SDK installed**
  ```bash
  dotnet --version
  # Expected: 10.0.x
  ```

- [ ] **Ports available:**
  - 7001 (Management API)
  - 7002 (Designer Dashboard)
  - 7100 (Demo.JsonApi)
  - 7200 (Demo.SoapApi)
  - 5432 (PostgreSQL)
  - 5672 (RabbitMQ)
  - 6379 (Redis)
  - 15000 (Aspire Dashboard)

### Configuration Files

- [ ] **appsettings.Development.json files present** in:
  - QuickApiMapper.Management.Api
  - QuickApiMapper.Designer.Web
  - Demo.JsonApi
  - Demo.SoapApi

- [ ] **Demo mode enabled** in Management API config:
  ```json
  "DemoMode": {
    "EnableDemoMode": true
  }
  ```

---

## Startup Validation

### 1. Start Aspire AppHost

- [ ] **Navigate to AppHost directory**
  ```bash
  cd src/QuickApiMapper.Host.AppHost
  ```

- [ ] **Start Aspire orchestrator**
  ```bash
  dotnet run
  ```

- [ ] **Verify startup logs show:**
  - "Building..." messages for all projects
  - "Now listening on:" for each service
  - No startup exceptions

**Expected Output:**
```
Building...
info: Aspire.Hosting.DistributedApplication[0]
      Aspire version: 10.0.x
      Application started: QuickApiMapper.Host.AppHost
      Hosting environment: Development
...
```

### 2. Verify Infrastructure Services

- [ ] **Open Aspire Dashboard**
  - Navigate to: http://localhost:15000
  - Should see all resources listed

- [ ] **Check PostgreSQL container**
  - Status: Running (green)
  - Health: Healthy
  - Port: 5432 exposed

- [ ] **Check RabbitMQ container**
  - Status: Running (green)
  - Health: Healthy
  - Port: 5672 exposed
  - Management UI: http://localhost:15672 (guest/guest)

- [ ] **Check Redis container**
  - Status: Running (green)
  - Health: Healthy
  - Port: 6379 exposed

### 3. Verify Application Services

- [ ] **Management API running**
  - URL: https://localhost:7001
  - Aspire Dashboard shows: Running
  - Logs show: "Now listening on https://localhost:7001"

- [ ] **Designer Dashboard running**
  - URL: https://localhost:7002
  - Aspire Dashboard shows: Running
  - Browser accessible (no certificate errors)

- [ ] **Demo.JsonApi running**
  - URL: http://localhost:7100
  - Aspire Dashboard shows: Running
  - Swagger UI accessible: http://localhost:7100/swagger

- [ ] **Demo.SoapApi running**
  - URL: http://localhost:7200
  - Aspire Dashboard shows: Running
  - WSDL accessible: http://localhost:7200/FulfillmentService.svc?wsdl

### 4. Database Initialization

- [ ] **Demo data seeded**
  - Check Management API logs for: "Demo data seeding completed"
  - No seeding errors in logs

- [ ] **Integrations created**
  - Navigate to: https://localhost:7002
  - Should see "Order Fulfillment Demo" integration listed

- [ ] **Sample messages exist**
  - Click on Message History
  - Should see 10 pre-seeded messages

---

## Functional Testing

### Test 1: Designer Dashboard Navigation

**Purpose:** Verify all dashboard pages load correctly

- [ ] **Home Page**
  - Navigate to: https://localhost:7002
  - Page loads without errors
  - Shows integration statistics card
  - Shows recent activity

- [ ] **Integrations List**
  - Click "Integrations" in sidebar
  - Shows "Order Fulfillment Demo" integration
  - Shows integration status (Active/Inactive)

- [ ] **Integration Detail**
  - Click on "Order Fulfillment Demo"
  - Shows integration details
  - Shows field mappings (16 mappings)
  - Shows transformers (3 transformers)

- [ ] **Message History**
  - Click "Message History" in sidebar
  - Shows table with 10+ messages
  - Can filter by direction (Inbound/Outbound)
  - Can filter by status (Success/Failed)

- [ ] **Settings Page**
  - Click "Settings" in sidebar
  - Shows Management API URL configuration
  - Shows theme toggle

### Test 2: Submit Demo Order (Happy Path)

**Purpose:** Test end-to-end order processing with valid data

**Test Data:**
```json
{
  "orderId": "TEST-001",
  "customerName": "Alice Johnson",
  "customerEmail": "ALICE.JOHNSON@EXAMPLE.COM",
  "orderDate": "2026-01-11T14:30:00Z",
  "totalAmount": 599.99,
  "currency": "USD",
  "items": [
    {
      "sku": "laptop-dell-xps15",
      "productName": "Dell XPS 15 Laptop",
      "quantity": 1,
      "unitPrice": 599.99
    }
  ],
  "shippingAddress": {
    "street": "123 Tech Lane",
    "city": "Seattle",
    "state": "WA",
    "postalCode": "98101",
    "country": "USA"
  },
  "priority": "EXPRESS"
}
```

**Steps:**

- [ ] **Submit order via Demo.JsonApi**
  ```bash
  curl -X POST http://localhost:7100/api/orders \
    -H "Content-Type: application/json" \
    -d @test-order.json
  ```

- [ ] **Verify HTTP 201 Created response**
  ```json
  {
    "orderId": "TEST-001",
    "status": "Pending",
    "createdAt": "2026-01-11T14:30:00Z"
  }
  ```

- [ ] **Check Designer Dashboard**
  - Navigate to Message History
  - New message appears (may take 1-2 seconds)
  - Direction: "Inbound"
  - Status: "Success"

- [ ] **View Message Detail**
  - Click on TEST-001 message
  - Shows Input Payload (JSON)
  - Shows Output Payload (SOAP/XML)

- [ ] **Verify Transformations**
  - **Email transformation:**
    - Input: `ALICE.JOHNSON@EXAMPLE.COM`
    - Output: `alice.johnson@example.com` (ToLower)

  - **SKU transformation:**
    - Input: `laptop-dell-xps15`
    - Output: `LAPTOP-DELL-XPS15` (ToUpper)

  - **Priority transformation:**
    - Input: `EXPRESS`
    - Output: `EXP` (MapValue)

- [ ] **Verify SOAP envelope structure**
  Output should contain:
  ```xml
  <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
    <soap:Body>
      <SubmitFulfillmentRequest>
        <OrderNumber>TEST-001</OrderNumber>
        <CustomerInfo>
          <ContactEmail>alice.johnson@example.com</ContactEmail>
        </CustomerInfo>
        <LineItems>
          <Item>
            <SKU>LAPTOP-DELL-XPS15</SKU>
            <Qty>1</Qty>
          </Item>
        </LineItems>
        <PriorityCode>EXP</PriorityCode>
      </SubmitFulfillmentRequest>
    </soap:Body>
  </soap:Envelope>
  ```

### Test 3: Submit Order with Multiple Items

**Purpose:** Test array mapping with `items[*]` notation

**Test Data:**
```json
{
  "orderId": "TEST-002",
  "customerName": "Bob Smith",
  "customerEmail": "bob@example.com",
  "items": [
    {"sku": "laptop-hp", "quantity": 2, "unitPrice": 499.99},
    {"sku": "mouse-logitech", "quantity": 2, "unitPrice": 29.99},
    {"sku": "keyboard-mechanical", "quantity": 1, "unitPrice": 89.99}
  ],
  "priority": "STANDARD"
}
```

**Steps:**

- [ ] **Submit multi-item order**
  ```bash
  curl -X POST http://localhost:7100/api/orders \
    -H "Content-Type: application/json" \
    -d @test-order-multi.json
  ```

- [ ] **Verify response**
  - HTTP 201 Created
  - orderId: TEST-002

- [ ] **Check transformed output**
  - Navigate to Message History → TEST-002
  - View Output Payload

- [ ] **Verify all items mapped**
  - Output should contain 3 `<Item>` elements
  - SKUs: `LAPTOP-HP`, `MOUSE-LOGITECH`, `KEYBOARD-MECHANICAL`
  - Quantities: 2, 2, 1

- [ ] **Verify STANDARD priority**
  - Input: `STANDARD`
  - Output: `STD` (MapValue)

### Test 4: Error Handling - Invalid Data

**Purpose:** Test validation and error reporting

**Test Data (Invalid Email):**
```json
{
  "orderId": "TEST-003",
  "customerEmail": "invalid-email",
  "items": []
}
```

**Steps:**

- [ ] **Submit invalid order**
  ```bash
  curl -X POST http://localhost:7100/api/orders \
    -H "Content-Type: application/json" \
    -d @test-order-invalid.json
  ```

- [ ] **Verify HTTP 400 Bad Request**
  - Response contains validation errors
  - Specific field errors listed

- [ ] **Check Dashboard**
  - Message may appear with Status: "Failed"
  - Error details captured

**Expected Validation Errors:**
- Invalid email format
- Items array cannot be empty

### Test 5: Management API Operations

**Purpose:** Test integration management via API

#### 5a. List Integrations

- [ ] **GET all integrations**
  ```bash
  curl https://localhost:7001/api/integrations
  ```

- [ ] **Verify response**
  - HTTP 200 OK
  - Contains "Order Fulfillment Demo"
  - Shows field mappings count
  - Shows transformers

#### 5b. Test Integration Mapping

- [ ] **Get integration ID** from list response

- [ ] **Submit test payload**
  ```bash
  curl -X POST https://localhost:7001/api/integrations/{id}/test \
    -H "Content-Type: application/json" \
    -d '{"samplePayload": "{\"orderId\":\"TEST-004\",\"priority\":\"EXPRESS\"}"}'
  ```

- [ ] **Verify test response**
  - Success: true
  - TransformedPayload contains SOAP XML
  - No errors

#### 5c. View Message Statistics

- [ ] **GET message statistics**
  ```bash
  curl https://localhost:7001/api/messages/statistics/{integrationId}
  ```

- [ ] **Verify statistics**
  - TotalMessages > 10 (seeded data)
  - SuccessCount matches
  - FailedCount if any errors
  - Average processing time reported

### Test 6: RabbitMQ Integration

**Purpose:** Test asynchronous message processing

**Prerequisites:**
- RabbitMQ running and accessible
- Queue configured for demo integration

**Steps:**

- [ ] **Access RabbitMQ Management UI**
  - Navigate to: http://localhost:15672
  - Login: guest / guest

- [ ] **Verify queues exist**
  - Should see queue for order processing
  - Check queue depth (messages waiting)

- [ ] **Publish message to queue**
  ```bash
  # Use RabbitMQ management UI or command:
  # Publish test message to order queue
  ```

- [ ] **Verify message consumed**
  - Queue depth decreases
  - Message appears in Dashboard history
  - Processed correctly

- [ ] **Check consumer logs**
  - Aspire Dashboard → RabbitMQ Consumer logs
  - Should show "Message received" and "Processing complete"

### Test 7: Demo Runner (Interactive Testing)

**Purpose:** Test the built-in demo runner UI

**Note:** Requires DemoRunner compilation errors to be fixed first

- [ ] **Navigate to Demo Runner**
  - Designer Dashboard → Demo Runner page

- [ ] **Select demo scenario**
  - Choose "Order Fulfillment"
  - Scenario details displayed

- [ ] **Run automated demo**
  - Click "Run Demo"
  - Progress indicators shown
  - Success/failure status displayed

- [ ] **View results**
  - Input payload shown
  - Output payload shown
  - Transformations highlighted

### Test 8: Performance Testing

**Purpose:** Verify transformation performance meets targets

**Target:** < 200ms per transformation

**Steps:**

- [ ] **Submit 10 orders rapidly**
  ```bash
  for i in {1..10}; do
    curl -X POST http://localhost:7100/api/orders \
      -H "Content-Type: application/json" \
      -d "{\"orderId\":\"PERF-$i\",\"priority\":\"EXPRESS\"}" &
  done
  wait
  ```

- [ ] **Check response times**
  - All responses < 300ms (includes network)
  - No timeouts
  - No failed requests

- [ ] **Verify message statistics**
  - Navigate to Dashboard → Integration Detail
  - Check "Average Processing Time"
  - Should be < 200ms

- [ ] **Monitor resource usage**
  - Aspire Dashboard → Resources
  - Memory: < 500MB per service
  - CPU: < 50% average

### Test 9: Data Persistence

**Purpose:** Verify data persists across restarts

**Steps:**

- [ ] **Note current message count**
  - Dashboard → Message History
  - Record total messages

- [ ] **Restart Management API**
  - Stop and restart via Aspire or Ctrl+C / `dotnet run`

- [ ] **Verify data preserved**
  - Message count unchanged
  - Integration configuration intact
  - No data loss

- [ ] **Submit new order**
  - Should continue incrementing from previous count

### Test 10: Demo Reset

**Purpose:** Test demo data reset functionality

**Steps:**

- [ ] **Access demo admin endpoint**
  ```bash
  curl -X POST https://localhost:7001/api/admin/demo/reset
  ```

- [ ] **Verify HTTP 200 OK**

- [ ] **Check Dashboard**
  - Message History cleared (except seeded 10)
  - Integration statistics reset
  - No data corruption

- [ ] **Verify seeded data restored**
  - 10 sample messages present
  - "Order Fulfillment Demo" integration exists

---

## Acceptance Criteria

The demo passes validation when **all** of the following are true:

### Build and Deployment
- ✅ Solution builds with 0 errors, 0 warnings
- ✅ All services start successfully via Aspire
- ✅ All infrastructure containers running and healthy

### Functionality
- ✅ Orders submit successfully via Demo.JsonApi
- ✅ Transformations apply correctly (ToLower, ToUpper, MapValue)
- ✅ SOAP output matches expected structure
- ✅ Multi-item orders process all items correctly
- ✅ Error handling works for invalid data

### Dashboard
- ✅ All pages load without errors
- ✅ Message history displays correctly
- ✅ Integration detail shows mappings and transformers
- ✅ Statistics update in real-time

### Performance
- ✅ Transformation latency < 200ms average
- ✅ Can handle 10 concurrent requests
- ✅ Memory usage < 500MB per service
- ✅ No memory leaks after 100+ requests

### Data Integrity
- ✅ Messages persist to database correctly
- ✅ Data survives service restart
- ✅ Demo reset works without corruption

---

## Known Limitations

Document any known limitations encountered during testing:

1. **Schema Import** - Returns mock data only (documented feature limitation)
2. **ServiceBus Worker** - Message processing not implemented (documented TODO)
3. **Settings Persistence** - User preferences don't save (documented enhancement)
4. **Dynamic Transformer Discovery** - Uses static list (documented enhancement)

---

## Test Environment

Record your test environment details:

| Component | Version | Notes |
|-----------|---------|-------|
| Operating System | | Windows/Linux/macOS |
| .NET SDK | | `dotnet --version` |
| Docker | | `docker --version` |
| Browser | | For Designer Dashboard |
| Date Tested | | YYYY-MM-DD |
| Tester Name | | |

---

## Issues Found During Testing

Record any issues encountered:

| # | Severity | Description | Status | Notes |
|---|----------|-------------|--------|-------|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

**Severity Levels:**
- **Critical:** Blocks demo completely
- **High:** Major functionality broken
- **Medium:** Feature works but with issues
- **Low:** Minor cosmetic or UX issue

---

## Sign-Off

### Tester Certification

I certify that I have completed all applicable tests in this checklist and documented all results accurately.

**Tester Name:** ______________________

**Date:** ______________________

**Signature:** ______________________

### Test Results Summary

- **Total Tests:** 10 test scenarios
- **Tests Passed:** ___ / 10
- **Tests Failed:** ___ / 10
- **Tests Blocked:** ___ / 10 (due to build errors)

**Overall Status:** ⬜ PASS | ⬜ FAIL | ⬜ BLOCKED

**Recommended Action:**
- ⬜ Approve for production demo
- ⬜ Fix issues and retest
- ⬜ Blocked - resolve build errors first

---

## Appendix: Quick Reference

### Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| Designer Dashboard | https://localhost:7002 | None |
| Management API | https://localhost:7001 | None |
| Demo.JsonApi | http://localhost:7100 | None |
| Demo.JsonApi Swagger | http://localhost:7100/swagger | None |
| Demo.SoapApi | http://localhost:7200 | None |
| Demo.SoapApi WSDL | http://localhost:7200/FulfillmentService.svc?wsdl | None |
| Aspire Dashboard | http://localhost:15000 | None |
| RabbitMQ Management | http://localhost:15672 | guest/guest |

### Sample cURL Commands

#### Submit Order
```bash
curl -X POST http://localhost:7100/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-001",
    "customerEmail": "test@example.com",
    "priority": "EXPRESS",
    "items": [{"sku": "laptop", "quantity": 1, "unitPrice": 999.99}]
  }'
```

#### Get Order
```bash
curl http://localhost:7100/api/orders/TEST-001
```

#### List Integrations
```bash
curl https://localhost:7001/api/integrations
```

#### Test Integration
```bash
curl -X POST https://localhost:7001/api/integrations/{id}/test \
  -H "Content-Type: application/json" \
  -d '{"samplePayload": "{\"orderId\":\"TEST\"}"}'
```

#### Get Message Statistics
```bash
curl https://localhost:7001/api/messages/statistics/{integrationId}
```

### Quick Diagnostic Commands

```bash
# Check all services running
docker ps

# View Management API logs
dotnet run --project src/QuickApiMapper.Management.Api

# View RabbitMQ queue status
docker exec rabbitmq rabbitmqctl list_queues

# Check PostgreSQL connection
docker exec postgres psql -U postgres -d quickapimapper -c "SELECT COUNT(*) FROM integrations;"

# Build specific project
dotnet build src/Demo.JsonApi/

# Run tests
dotnet test tests/QuickApiMapper.UnitTests/
```

---

**END OF CHECKLIST**
