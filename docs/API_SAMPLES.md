# QuickApiMapper Demo API Samples

Comprehensive collection of API samples, cURL commands, and integration testing examples for the QuickApiMapper demo.

## Table of Contents

- [Overview](#overview)
- [Demo Endpoints](#demo-endpoints)
- [cURL Commands](#curl-commands)
- [Postman Collection](#postman-collection)
- [Integration Testing](#integration-testing)
- [RabbitMQ Examples](#rabbitmq-examples)
- [Sample Payloads](#sample-payloads)

## Overview

This document provides ready-to-use API samples for testing and demonstrating QuickApiMapper's capabilities. All samples use the pre-seeded demo integrations.

### Base URLs

| Service | HTTP | HTTPS |
|---------|------|-------|
| QuickApiMapper Web | http://localhost:5000 | https://localhost:7000 |
| Management API | http://localhost:7001 | https://localhost:7001 |
| Designer Dashboard | http://localhost:7002 | https://localhost:7002 |
| Demo.JsonApi | http://localhost:5100 | https://localhost:7100 |
| Demo.SoapApi | http://localhost:5101 | https://localhost:7101 |

## Demo Endpoints

### 1. JSON to SOAP Order Processing

**Endpoint**: `POST /api/demo/fulfillment/submit`

**Purpose**: Transform JSON orders to SOAP fulfillment requests

**Request Format**: JSON
**Response Format**: SOAP (proxied back as JSON confirmation)

### 2. SOAP to JSON Fulfillment Status

**Endpoint**: `POST /api/demo/fulfillment/status`

**Purpose**: Transform SOAP status responses to JSON

**Request Format**: SOAP XML
**Response Format**: JSON

### 3. RabbitMQ Order Batch Processing

**Queue**: `quickapi.demo.orders`

**Purpose**: Async order processing via message queue

**Message Format**: JSON (same as endpoint #1)

## cURL Commands

### Basic JSON to SOAP Transformation

#### Standard Priority Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-001",
    "customerName": "John Smith",
    "customerEmail": "JOHN.SMITH@EXAMPLE.COM",
    "orderDate": "2026-01-11T14:30:00Z",
    "totalAmount": 599.99,
    "currency": "USD",
    "items": [
      {
        "sku": "laptop-xps15",
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
  }'
```

**Expected Response** (HTTP 200):
```json
{
  "success": true,
  "confirmationNumber": "WH-20260111-A1B2C3D4",
  "orderId": "ORD-2026-001",
  "processedDateTime": "2026-01-11T14:30:05Z",
  "status": "PENDING",
  "message": "Fulfillment request accepted and queued for processing",
  "estimatedShipDate": "2026-01-14T14:30:05Z"
}
```

#### Express Priority Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-002",
    "customerName": "Jane Anderson",
    "customerEmail": "jane.anderson@example.com",
    "orderDate": "2026-01-11T15:00:00Z",
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
      "street": "456 Innovation Dr",
      "city": "San Francisco",
      "state": "CA",
      "postalCode": "94102",
      "country": "USA"
    },
    "priority": "EXPRESS"
  }'
```

**Key Difference**: Priority mapping `EXPRESS` → `EXP` in SOAP output

#### Overnight Priority Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-003",
    "customerName": "Robert Johnson",
    "customerEmail": "ROBERT.JOHNSON@BIGCORP.COM",
    "orderDate": "2026-01-11T16:00:00Z",
    "totalAmount": 2499.99,
    "currency": "USD",
    "items": [
      {
        "sku": "server-dell-r740",
        "productName": "Dell PowerEdge R740 Server",
        "quantity": 1,
        "unitPrice": 2499.99
      }
    ],
    "shippingAddress": {
      "street": "789 Enterprise Blvd",
      "city": "Austin",
      "state": "TX",
      "postalCode": "73301",
      "country": "USA"
    },
    "priority": "OVERNIGHT"
  }'
```

**Key Difference**: Priority mapping `OVERNIGHT` → `OVN` in SOAP output

### Multi-Item Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-004",
    "customerName": "Sarah Williams",
    "customerEmail": "sarah.williams@example.com",
    "orderDate": "2026-01-11T17:00:00Z",
    "totalAmount": 1847.94,
    "currency": "USD",
    "items": [
      {
        "sku": "monitor-dell-27",
        "productName": "Dell 27-inch Monitor",
        "quantity": 2,
        "unitPrice": 399.99
      },
      {
        "sku": "keyboard-logitech",
        "productName": "Logitech Mechanical Keyboard",
        "quantity": 2,
        "unitPrice": 149.99
      },
      {
        "sku": "mouse-mx-master",
        "productName": "Logitech MX Master Mouse",
        "quantity": 2,
        "unitPrice": 99.99
      },
      {
        "sku": "webcam-logitech-4k",
        "productName": "Logitech 4K Webcam",
        "quantity": 1,
        "unitPrice": 199.99
      },
      {
        "sku": "headset-jabra",
        "productName": "Jabra Evolve2 Headset",
        "quantity": 2,
        "unitPrice": 249.99
      }
    ],
    "shippingAddress": {
      "street": "321 Office Park Lane",
      "city": "Boston",
      "state": "MA",
      "postalCode": "02101",
      "country": "USA"
    },
    "priority": "STANDARD"
  }'
```

**Key Feature**: Demonstrates array transformation - multiple items mapped to `<LineItems><Item>...</Item></LineItems>`

### International Order

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-2026-005",
    "customerName": "Hans Müller",
    "customerEmail": "hans.mueller@example.de",
    "orderDate": "2026-01-11T18:00:00Z",
    "totalAmount": 899.99,
    "currency": "EUR",
    "items": [
      {
        "sku": "laptop-thinkpad-x1",
        "productName": "Lenovo ThinkPad X1 Carbon",
        "quantity": 1,
        "unitPrice": 899.99
      }
    ],
    "shippingAddress": {
      "street": "Hauptstraße 123",
      "city": "Berlin",
      "state": "Berlin",
      "postalCode": "10115",
      "country": "Germany"
    },
    "priority": "EXPRESS"
  }'
```

**Key Feature**: Demonstrates international addresses and currency handling

### Using File Input

Save a JSON order to `order.json`, then:

```bash
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d @order.json
```

### Management API - List Integrations

```bash
curl https://localhost:7001/api/integrations
```

**Expected Response**:
```json
[
  {
    "id": 1,
    "name": "Demo: JSON to SOAP Order Processing",
    "endpoint": "/api/demo/fulfillment/submit",
    "sourceType": "JSON",
    "destinationType": "SOAP",
    "destinationUrl": "http://demo-soapapi/WarehouseService.asmx",
    "isActive": true,
    "enableMessageCapture": true
  },
  {
    "id": 2,
    "name": "Demo: SOAP to JSON Fulfillment Status",
    ...
  },
  {
    "id": 3,
    "name": "Demo: RabbitMQ Order Batch Processing",
    ...
  }
]
```

### Management API - Get Specific Integration

```bash
curl https://localhost:7001/api/integrations/1
```

### Management API - Get Field Mappings

```bash
curl https://localhost:7001/api/integrations/1/mappings
```

**Expected Response**: Array of 16 field mappings with sources, destinations, and transformers

### Demo.JsonApi - Get All Orders

```bash
curl https://localhost:7100/api/orders
```

**Expected Response**: Array of 10 pre-seeded orders

### Demo.JsonApi - Get Specific Order

```bash
curl https://localhost:7100/api/orders/ORD-2026-001
```

### Demo.SoapApi - Get WSDL

```bash
curl http://localhost:5101/WarehouseService.asmx?wsdl
```

**Expected Response**: Complete WSDL document for the warehouse service

## Postman Collection

### Import Instructions

1. Open Postman
2. Click **Import** button
3. Choose **Raw Text** tab
4. Paste the JSON below
5. Click **Import**

### Postman Collection JSON

```json
{
  "info": {
    "name": "QuickApiMapper Demo",
    "description": "Complete demo collection for QuickApiMapper",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "webUrl",
      "value": "http://localhost:5000"
    },
    {
      "key": "managementUrl",
      "value": "https://localhost:7001"
    },
    {
      "key": "jsonApiUrl",
      "value": "https://localhost:7100"
    }
  ],
  "item": [
    {
      "name": "QuickApiMapper Web",
      "item": [
        {
          "name": "JSON to SOAP - Standard Order",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"orderId\": \"ORD-{{$randomInt}}\",\n  \"customerName\": \"{{$randomFullName}}\",\n  \"customerEmail\": \"{{$randomEmail}}\",\n  \"orderDate\": \"{{$isoTimestamp}}\",\n  \"totalAmount\": 599.99,\n  \"currency\": \"USD\",\n  \"items\": [\n    {\n      \"sku\": \"laptop-xps15\",\n      \"productName\": \"Dell XPS 15 Laptop\",\n      \"quantity\": 1,\n      \"unitPrice\": 599.99\n    }\n  ],\n  \"shippingAddress\": {\n    \"street\": \"{{$randomStreetAddress}}\",\n    \"city\": \"{{$randomCity}}\",\n    \"state\": \"{{$randomCountryCode}}\",\n    \"postalCode\": \"{{$randomInt}}\",\n    \"country\": \"USA\"\n  },\n  \"priority\": \"STANDARD\"\n}"
            },
            "url": {
              "raw": "{{webUrl}}/api/demo/fulfillment/submit",
              "host": ["{{webUrl}}"],
              "path": ["api", "demo", "fulfillment", "submit"]
            }
          }
        },
        {
          "name": "JSON to SOAP - Express Order",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"orderId\": \"ORD-{{$randomInt}}\",\n  \"customerName\": \"{{$randomFullName}}\",\n  \"customerEmail\": \"{{$randomEmail}}\",\n  \"orderDate\": \"{{$isoTimestamp}}\",\n  \"totalAmount\": 1299.99,\n  \"currency\": \"USD\",\n  \"items\": [\n    {\n      \"sku\": \"laptop-macbook-pro\",\n      \"productName\": \"MacBook Pro 16-inch\",\n      \"quantity\": 1,\n      \"unitPrice\": 1299.99\n    }\n  ],\n  \"shippingAddress\": {\n    \"street\": \"{{$randomStreetAddress}}\",\n    \"city\": \"{{$randomCity}}\",\n    \"state\": \"{{$randomCountryCode}}\",\n    \"postalCode\": \"{{$randomInt}}\",\n    \"country\": \"USA\"\n  },\n  \"priority\": \"EXPRESS\"\n}"
            },
            "url": {
              "raw": "{{webUrl}}/api/demo/fulfillment/submit",
              "host": ["{{webUrl}}"],
              "path": ["api", "demo", "fulfillment", "submit"]
            }
          }
        },
        {
          "name": "JSON to SOAP - Overnight Order",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"orderId\": \"ORD-{{$randomInt}}\",\n  \"customerName\": \"{{$randomFullName}}\",\n  \"customerEmail\": \"{{$randomEmail}}\",\n  \"orderDate\": \"{{$isoTimestamp}}\",\n  \"totalAmount\": 2499.99,\n  \"currency\": \"USD\",\n  \"items\": [\n    {\n      \"sku\": \"server-dell-r740\",\n      \"productName\": \"Dell PowerEdge R740 Server\",\n      \"quantity\": 1,\n      \"unitPrice\": 2499.99\n    }\n  ],\n  \"shippingAddress\": {\n    \"street\": \"{{$randomStreetAddress}}\",\n    \"city\": \"{{$randomCity}}\",\n    \"state\": \"{{$randomCountryCode}}\",\n    \"postalCode\": \"{{$randomInt}}\",\n    \"country\": \"USA\"\n  },\n  \"priority\": \"OVERNIGHT\"\n}"
            },
            "url": {
              "raw": "{{webUrl}}/api/demo/fulfillment/submit",
              "host": ["{{webUrl}}"],
              "path": ["api", "demo", "fulfillment", "submit"]
            }
          }
        }
      ]
    },
    {
      "name": "Management API",
      "item": [
        {
          "name": "List All Integrations",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{managementUrl}}/api/integrations",
              "host": ["{{managementUrl}}"],
              "path": ["api", "integrations"]
            }
          }
        },
        {
          "name": "Get Integration by ID",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{managementUrl}}/api/integrations/1",
              "host": ["{{managementUrl}}"],
              "path": ["api", "integrations", "1"]
            }
          }
        },
        {
          "name": "Get Field Mappings",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{managementUrl}}/api/integrations/1/mappings",
              "host": ["{{managementUrl}}"],
              "path": ["api", "integrations", "1", "mappings"]
            }
          }
        }
      ]
    },
    {
      "name": "Demo.JsonApi",
      "item": [
        {
          "name": "Get All Orders",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{jsonApiUrl}}/api/orders",
              "host": ["{{jsonApiUrl}}"],
              "path": ["api", "orders"]
            }
          }
        },
        {
          "name": "Get Order by ID",
          "request": {
            "method": "GET",
            "url": {
              "raw": "{{jsonApiUrl}}/api/orders/ORD-2026-001",
              "host": ["{{jsonApiUrl}}"],
              "path": ["api", "orders", "ORD-2026-001"]
            }
          }
        },
        {
          "name": "Create New Order",
          "request": {
            "method": "POST",
            "header": [
              {
                "key": "Content-Type",
                "value": "application/json"
              }
            ],
            "body": {
              "mode": "raw",
              "raw": "{\n  \"customerName\": \"{{$randomFullName}}\",\n  \"customerEmail\": \"{{$randomEmail}}\",\n  \"totalAmount\": 599.99,\n  \"currency\": \"USD\",\n  \"items\": [\n    {\n      \"sku\": \"TEST-001\",\n      \"productName\": \"Test Product\",\n      \"quantity\": 1,\n      \"unitPrice\": 599.99\n    }\n  ],\n  \"shippingAddress\": {\n    \"street\": \"{{$randomStreetAddress}}\",\n    \"city\": \"{{$randomCity}}\",\n    \"state\": \"CA\",\n    \"postalCode\": \"90001\",\n    \"country\": \"USA\"\n  },\n  \"priority\": \"STANDARD\"\n}"
            },
            "url": {
              "raw": "{{jsonApiUrl}}/api/orders",
              "host": ["{{jsonApiUrl}}"],
              "path": ["api", "orders"]
            }
          }
        }
      ]
    }
  ]
}
```

### Using Postman Variables

The collection uses Postman dynamic variables:
- `{{$randomInt}}` - Random integer
- `{{$randomFullName}}` - Random full name
- `{{$randomEmail}}` - Random email
- `{{$randomStreetAddress}}` - Random street address
- `{{$randomCity}}` - Random city
- `{{$isoTimestamp}}` - Current ISO timestamp

## Integration Testing

### Automated Test Suite (Bash)

Save as `test-demo.sh`:

```bash
#!/bin/bash

BASE_URL="http://localhost:5000"
TOTAL_TESTS=0
PASSED_TESTS=0

test_case() {
  TOTAL_TESTS=$((TOTAL_TESTS + 1))
  echo "Test $TOTAL_TESTS: $1"

  RESPONSE=$(curl -s -w "\n%{http_code}" "$@")
  HTTP_CODE=$(echo "$RESPONSE" | tail -n 1)
  BODY=$(echo "$RESPONSE" | head -n -1)

  if [ "$HTTP_CODE" -eq 200 ]; then
    echo "  ✓ PASSED (HTTP $HTTP_CODE)"
    PASSED_TESTS=$((PASSED_TESTS + 1))
  else
    echo "  ✗ FAILED (HTTP $HTTP_CODE)"
    echo "  Response: $BODY"
  fi
  echo ""
}

echo "======================================"
echo "QuickApiMapper Demo Integration Tests"
echo "======================================"
echo ""

# Test 1: Standard Priority Order
test_case "Standard Priority Order" \
  -X POST "$BASE_URL/api/demo/fulfillment/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-001",
    "customerName": "Test User",
    "customerEmail": "test@example.com",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 100.00,
    "currency": "USD",
    "items": [{"sku": "test", "productName": "Test", "quantity": 1, "unitPrice": 100.00}],
    "shippingAddress": {"street": "123 Test", "city": "Test", "state": "CA", "postalCode": "90001", "country": "USA"},
    "priority": "STANDARD"
  }'

# Test 2: Express Priority Order
test_case "Express Priority Order" \
  -X POST "$BASE_URL/api/demo/fulfillment/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-002",
    "customerName": "Test User",
    "customerEmail": "test@example.com",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 200.00,
    "currency": "USD",
    "items": [{"sku": "test", "productName": "Test", "quantity": 1, "unitPrice": 200.00}],
    "shippingAddress": {"street": "123 Test", "city": "Test", "state": "CA", "postalCode": "90001", "country": "USA"},
    "priority": "EXPRESS"
  }'

# Test 3: Overnight Priority Order
test_case "Overnight Priority Order" \
  -X POST "$BASE_URL/api/demo/fulfillment/submit" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-003",
    "customerName": "Test User",
    "customerEmail": "test@example.com",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 300.00,
    "currency": "USD",
    "items": [{"sku": "test", "productName": "Test", "quantity": 1, "unitPrice": 300.00}],
    "shippingAddress": {"street": "123 Test", "city": "Test", "state": "CA", "postalCode": "90001", "country": "USA"},
    "priority": "OVERNIGHT"
  }'

echo "======================================"
echo "Test Summary"
echo "======================================"
echo "Total Tests: $TOTAL_TESTS"
echo "Passed: $PASSED_TESTS"
echo "Failed: $((TOTAL_TESTS - PASSED_TESTS))"
echo ""

if [ $PASSED_TESTS -eq $TOTAL_TESTS ]; then
  echo "✓ All tests passed!"
  exit 0
else
  echo "✗ Some tests failed"
  exit 1
fi
```

**Run**:
```bash
chmod +x test-demo.sh
./test-demo.sh
```

### PowerShell Test Suite

Save as `Test-Demo.ps1`:

```powershell
$baseUrl = "http://localhost:5000"
$totalTests = 0
$passedTests = 0

function Test-Endpoint {
    param(
        [string]$TestName,
        [string]$Url,
        [string]$Method = "POST",
        [object]$Body
    )

    $script:totalTests++
    Write-Host "Test $script:totalTests: $TestName" -ForegroundColor Cyan

    try {
        $response = Invoke-RestMethod -Uri $Url -Method $Method -Body ($Body | ConvertTo-Json -Depth 10) -ContentType "application/json"
        Write-Host "  ✓ PASSED" -ForegroundColor Green
        $script:passedTests++
    }
    catch {
        Write-Host "  ✗ FAILED: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "QuickApiMapper Demo Integration Tests" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host ""

# Test 1: Standard Order
Test-Endpoint -TestName "Standard Priority Order" -Url "$baseUrl/api/demo/fulfillment/submit" -Body @{
    orderId = "TEST-001"
    customerName = "Test User"
    customerEmail = "test@example.com"
    orderDate = "2026-01-11T10:00:00Z"
    totalAmount = 100.00
    currency = "USD"
    items = @(
        @{
            sku = "test"
            productName = "Test Product"
            quantity = 1
            unitPrice = 100.00
        }
    )
    shippingAddress = @{
        street = "123 Test St"
        city = "Test City"
        state = "CA"
        postalCode = "90001"
        country = "USA"
    }
    priority = "STANDARD"
}

# Test 2: Express Order
Test-Endpoint -TestName "Express Priority Order" -Url "$baseUrl/api/demo/fulfillment/submit" -Body @{
    orderId = "TEST-002"
    customerName = "Test User"
    customerEmail = "test@example.com"
    orderDate = "2026-01-11T10:00:00Z"
    totalAmount = 200.00
    currency = "USD"
    items = @(
        @{
            sku = "test"
            productName = "Test Product"
            quantity = 1
            unitPrice = 200.00
        }
    )
    shippingAddress = @{
        street = "123 Test St"
        city = "Test City"
        state = "CA"
        postalCode = "90001"
        country = "USA"
    }
    priority = "EXPRESS"
}

Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "Test Summary" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "Total Tests: $totalTests"
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host ""

if ($passedTests -eq $totalTests) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    exit 1
}
```

**Run**:
```powershell
.\Test-Demo.ps1
```

## RabbitMQ Examples

### Publishing Messages with RabbitMQ Client

#### Using C# Console App

```csharp
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Declare exchange
channel.ExchangeDeclare("quickapi.exchange", "direct", durable: true);

// Create order message
var order = new
{
    orderId = "ORD-RABBITMQ-001",
    customerName = "RabbitMQ Test",
    customerEmail = "rabbitmq@example.com",
    orderDate = DateTime.UtcNow,
    totalAmount = 599.99,
    currency = "USD",
    items = new[]
    {
        new
        {
            sku = "test-product",
            productName = "Test Product",
            quantity = 1,
            unitPrice = 599.99
        }
    },
    shippingAddress = new
    {
        street = "123 Queue St",
        city = "Message City",
        state = "MQ",
        postalCode = "00000",
        country = "USA"
    },
    priority = "STANDARD"
};

var json = JsonSerializer.Serialize(order);
var body = Encoding.UTF8.GetBytes(json);

// Set properties
var properties = channel.CreateBasicProperties();
properties.Headers = new Dictionary<string, object>
{
    ["IntegrationName"] = "Demo: RabbitMQ Order Batch Processing"
};
properties.CorrelationId = Guid.NewGuid().ToString();
properties.Persistent = true;

// Publish
channel.BasicPublish(
    exchange: "quickapi.exchange",
    routingKey: "demo.orders",
    basicProperties: properties,
    body: body);

Console.WriteLine($"Message published with correlation ID: {properties.CorrelationId}");
```

#### Using Python (pika)

```python
import pika
import json
from datetime import datetime
import uuid

# Connect to RabbitMQ
connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
channel = connection.channel()

# Declare exchange
channel.exchange_declare(exchange='quickapi.exchange', exchange_type='direct', durable=True)

# Create order message
order = {
    "orderId": "ORD-PYTHON-001",
    "customerName": "Python Test",
    "customerEmail": "python@example.com",
    "orderDate": datetime.utcnow().isoformat() + "Z",
    "totalAmount": 899.99,
    "currency": "USD",
    "items": [
        {
            "sku": "python-test",
            "productName": "Python Test Product",
            "quantity": 1,
            "unitPrice": 899.99
        }
    ],
    "shippingAddress": {
        "street": "123 Python Lane",
        "city": "Script City",
        "state": "PY",
        "postalCode": "00000",
        "country": "USA"
    },
    "priority": "EXPRESS"
}

# Set properties
properties = pika.BasicProperties(
    headers={'IntegrationName': 'Demo: RabbitMQ Order Batch Processing'},
    correlation_id=str(uuid.uuid4()),
    delivery_mode=2  # persistent
)

# Publish
channel.basic_publish(
    exchange='quickapi.exchange',
    routing_key='demo.orders',
    body=json.dumps(order),
    properties=properties)

print(f"Message published with correlation ID: {properties.correlation_id}")

connection.close()
```

### Using RabbitMQ Management HTTP API

```bash
curl -u guest:guest -X POST http://localhost:15672/api/exchanges/%2F/quickapi.exchange/publish \
  -H "Content-Type: application/json" \
  -d '{
    "properties": {
      "headers": {
        "IntegrationName": "Demo: RabbitMQ Order Batch Processing"
      },
      "correlation_id": "http-api-test-001",
      "delivery_mode": 2
    },
    "routing_key": "demo.orders",
    "payload": "{\"orderId\":\"ORD-HTTP-001\",\"customerName\":\"HTTP Test\",\"customerEmail\":\"http@example.com\",\"orderDate\":\"2026-01-11T10:00:00Z\",\"totalAmount\":699.99,\"currency\":\"USD\",\"items\":[{\"sku\":\"http-test\",\"productName\":\"HTTP Test Product\",\"quantity\":1,\"unitPrice\":699.99}],\"shippingAddress\":{\"street\":\"123 HTTP Blvd\",\"city\":\"API City\",\"state\":\"HT\",\"postalCode\":\"00000\",\"country\":\"USA\"},\"priority\":\"OVERNIGHT\"}",
    "payload_encoding": "string"
  }'
```

## Sample Payloads

### Minimal Valid Order

```json
{
  "orderId": "MIN-001",
  "customerName": "Min Test",
  "customerEmail": "min@test.com",
  "orderDate": "2026-01-11T10:00:00Z",
  "totalAmount": 1.00,
  "currency": "USD",
  "items": [
    {
      "sku": "min",
      "productName": "Minimal",
      "quantity": 1,
      "unitPrice": 1.00
    }
  ],
  "shippingAddress": {
    "street": "1 St",
    "city": "City",
    "state": "ST",
    "postalCode": "00000",
    "country": "USA"
  },
  "priority": "STANDARD"
}
```

### Complex Order (Large Payload)

```json
{
  "orderId": "COMPLEX-001",
  "customerName": "Enterprise Customer Inc.",
  "customerEmail": "procurement@enterprise.com",
  "orderDate": "2026-01-11T10:00:00Z",
  "totalAmount": 45678.89,
  "currency": "USD",
  "items": [
    {
      "sku": "SERVER-DELL-R740-001",
      "productName": "Dell PowerEdge R740 Server - Dual Xeon Gold 6248R, 256GB RAM, 8TB SSD RAID 10",
      "quantity": 5,
      "unitPrice": 7999.99
    },
    {
      "sku": "SWITCH-CISCO-48P-001",
      "productName": "Cisco Catalyst 9300 48-Port Gigabit Switch with 10G Uplinks",
      "quantity": 3,
      "unitPrice": 8999.99
    },
    {
      "sku": "FIREWALL-PALO-001",
      "productName": "Palo Alto PA-850 Next-Gen Firewall",
      "quantity": 2,
      "unitPrice": 12999.99
    },
    {
      "sku": "UPS-APC-5000-001",
      "productName": "APC Smart-UPS 5000VA Rack-Mount with Network Management",
      "quantity": 2,
      "unitPrice": 3499.99
    },
    {
      "sku": "CABLE-CAT6A-100-001",
      "productName": "Cat6A Ethernet Cable 100ft (Box of 50)",
      "quantity": 10,
      "unitPrice": 89.99
    }
  ],
  "shippingAddress": {
    "street": "1000 Enterprise Boulevard, Building 5, Loading Dock C",
    "city": "San Jose",
    "state": "CA",
    "postalCode": "95134",
    "country": "USA"
  },
  "priority": "EXPRESS"
}
```

### Edge Cases

#### Special Characters in Name

```json
{
  "orderId": "EDGE-001",
  "customerName": "O'Brien & Sons, Inc. (François Müller, CEO)",
  "customerEmail": "francois.muller@obrien-and-sons.com",
  "...": "..."
}
```

#### Very Long Address

```json
{
  "shippingAddress": {
    "street": "The Old Victorian Manor House at the End of the Long and Winding Country Road, Unit 123, Building B, Through the Large Iron Gates",
    "city": "San Francisco",
    "state": "CA",
    "postalCode": "94102",
    "country": "USA"
  }
}
```

---

**Document Version**: 1.0
**Last Updated**: 2026-01-11
**Related Documents**: [DEMO_GUIDE.md](DEMO_GUIDE.md), [DEMO_QUICK_REFERENCE.md](DEMO_QUICK_REFERENCE.md)
