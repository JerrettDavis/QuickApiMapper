# REST/JSON Protocol

This guide covers using QuickApiMapper with REST APIs and JSON payloads.

## Overview

REST (Representational State Transfer) with JSON is the most common integration pattern for modern APIs. QuickApiMapper provides first-class support for:

- **JSON Source** - Parsing incoming JSON requests
- **JSON Destination** - Generating JSON for outbound REST calls
- **JSONPath** - Powerful query language for JSON navigation
- **HTTP Methods** - Support for GET, POST, PUT, PATCH, DELETE

## JSON Source Configuration

When your integration receives JSON input, use the JSON source resolver.

### Source Type

Set the source type to `JSON` when creating the integration:

```json
{
 "sourceType": "JSON"
}
```

No additional source configuration is required - the JSON source resolver is automatically used.

### JSONPath Syntax

Use JSONPath to navigate JSON structures:

#### Basic Path

```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe"
 }
}
```

**Paths**:
- `$.customer.firstName` → "John"
- `$.customer.lastName` → "Doe"

#### Nested Objects

```json
{
 "order": {
 "customer": {
 "address": {
 "city": "Boston"
 }
 }
 }
}
```

**Path**: `$.order.customer.address.city` → "Boston"

#### Arrays

```json
{
 "items": [
 {"name": "Item 1", "price": 10},
 {"name": "Item 2", "price": 20}
 ]
}
```

**Paths**:
- `$.items[0].name` → "Item 1"
- `$.items[1].price` → 20
- `$.items[*].name` → ["Item 1", "Item 2"]

See [Field Mappings](../field-mappings.md) for complete JSONPath documentation.

## JSON Destination Configuration

When sending JSON to a REST API, configure the destination accordingly.

### Destination Type

Set the destination type to `JSON`:

```json
{
 "destinationType": "JSON",
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers",
 "method": "POST",
 "contentType": "application/json"
 }
}
```

### HTTP Methods

#### POST - Create Resources

```json
{
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers",
 "method": "POST"
 }
}
```

**Request**:
```http
POST https://api.example.com/customers
Content-Type: application/json

{
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com"
}
```

#### PUT - Update Resources (Full Replace)

```json
{
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers/{customerId}",
 "method": "PUT"
 }
}
```

**Request**:
```http
PUT https://api.example.com/customers/C12345
Content-Type: application/json

{
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com"
}
```

#### PATCH - Partial Updates

```json
{
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers/{customerId}",
 "method": "PATCH"
 }
}
```

**Request**:
```http
PATCH https://api.example.com/customers/C12345
Content-Type: application/json

{
 "email": "newemail@example.com"
}
```

#### DELETE - Remove Resources

```json
{
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers/{customerId}",
 "method": "DELETE"
 }
}
```

**Request**:
```http
DELETE https://api.example.com/customers/C12345
```

### URL Parameters

Use placeholders in the endpoint URL:

```json
{
 "destinationConfig": {
 "endpointUrl": "https://api.example.com/customers/{customerId}/orders/{orderId}",
 "method": "GET"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.customer.id",
 "destinationPath": "__url.customerId"
 },
 {
 "sourcePath": "$.order.id",
 "destinationPath": "__url.orderId"
 }
 ]
}
```

**Result**: `GET https://api.example.com/customers/C12345/orders/ORD-001`

## Complete Example: JSON to JSON

### Scenario

Transform customer data from Salesforce format to ERP format.

### Source (Salesforce JSON)

```json
{
 "Account": {
 "Id": "001xx000003DGbwAAG",
 "Name": "Acme Corporation",
 "BillingStreet": "123 Main St",
 "BillingCity": "Boston",
 "BillingState": "MA",
 "BillingPostalCode": "02101",
 "Phone": "5551234567",
 "Type": "Customer - Direct",
 "Industry": "Technology"
 }
}
```

### Integration Configuration

```json
{
 "name": "Salesforce to ERP Customer",
 "integrationId": "sf-to-ifs-customer",
 "sourceType": "JSON",
 "destinationType": "JSON",
 "destinationConfig": {
 "endpointUrl": "https://ifs.example.com/api/v1/customers",
 "method": "POST"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.Account.Id",
 "destinationPath": "Customer.ExternalId"
 },
 {
 "sourcePath": "$.Account.Name",
 "destinationPath": "Customer.Name"
 },
 {
 "sourcePath": "$.Account.BillingStreet",
 "destinationPath": "Customer.Address.Street"
 },
 {
 "sourcePath": "$.Account.BillingCity",
 "destinationPath": "Customer.Address.City"
 },
 {
 "sourcePath": "$.Account.BillingState",
 "destinationPath": "Customer.Address.State"
 },
 {
 "sourcePath": "$.Account.BillingPostalCode",
 "destinationPath": "Customer.Address.PostalCode"
 },
 {
 "sourcePath": "$.Account.Phone",
 "destinationPath": "Customer.Phone",
 "transformers": [
 {"transformerName": "FormatPhone"}
 ]
 },
 {
 "sourcePath": "$.Account.Industry",
 "destinationPath": "Customer.Industry"
 }
 ],
 "staticValues": [
 {
 "destinationPath": "Customer.Source",
 "staticValue": "Salesforce"
 },
 {
 "destinationPath": "Customer.Type",
 "staticValue": "B2B"
 }
 ]
}
```

### Output (ERP JSON)

```json
{
 "Customer": {
 "ExternalId": "001xx000003DGbwAAG",
 "Name": "Acme Corporation",
 "Address": {
 "Street": "123 Main St",
 "City": "Boston",
 "State": "MA",
 "PostalCode": "02101"
 },
 "Phone": "(555) 123-4567",
 "Industry": "Technology",
 "Source": "Salesforce",
 "Type": "B2B"
 }
}
```

## Authentication

REST APIs typically use one of several authentication methods.

### Bearer Token (OAuth 2.0)

```json
{
 "authenticationConfig": {
 "type": "Bearer",
 "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
 }
}
```

**Request Header**:
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### API Key

```json
{
 "authenticationConfig": {
 "type": "ApiKey",
 "headerName": "X-API-Key",
 "apiKey": "your-api-key-here"
 }
}
```

**Request Header**:
```http
X-API-Key: your-api-key-here
```

### Basic Authentication

```json
{
 "authenticationConfig": {
 "type": "Basic",
 "username": "user@example.com",
 "password": "password123"
 }
}
```

**Request Header**:
```http
Authorization: Basic dXNlckBleGFtcGxlLmNvbTpwYXNzd29yZDEyMw==
```

### Custom Headers

```json
{
 "destinationConfig": {
 "customHeaders": {
 "X-Client-Id": "quickapimapper",
 "X-Request-Id": "{correlationId}",
 "X-Tenant-Id": "tenant-123"
 }
 }
}
```

## Error Handling

Handle REST API errors appropriately.

### HTTP Status Codes

QuickApiMapper automatically handles standard HTTP status codes:

- **2xx Success** - Request succeeded
- **4xx Client Error** - Invalid request (logged as error)
- **5xx Server Error** - Server failure (can trigger retry)

### Retry on Transient Errors

Configure retry for temporary failures:

```json
{
 "destinationConfig": {
 "retryPolicy": {
 "maxRetries": 3,
 "retryOn": [408, 429, 500, 502, 503, 504],
 "backoffMs": [1000, 2000, 4000]
 }
 }
}
```

**Status Codes**:
- `408` - Request Timeout
- `429` - Too Many Requests (rate limit)
- `500` - Internal Server Error
- `502` - Bad Gateway
- `503` - Service Unavailable
- `504` - Gateway Timeout

### Error Response Handling

Capture error details from the response:

```json
{
 "destinationConfig": {
 "errorHandling": {
 "captureErrorResponse": true,
 "errorPathInResponse": "$.error.message"
 }
 }
}
```

**Error Response**:
```json
{
 "error": {
 "code": "VALIDATION_ERROR",
 "message": "Email address is invalid",
 "field": "email"
 }
}
```

## Content Negotiation

### Request Content Type

Specify the content type for requests:

```json
{
 "destinationConfig": {
 "contentType": "application/json"
 }
}
```

Common content types:
- `application/json` - Standard JSON
- `application/vnd.api+json` - JSON API format
- `application/ld+json` - JSON-LD (Linked Data)

### Accept Header

Specify accepted response format:

```json
{
 "destinationConfig": {
 "accept": "application/json"
 }
}
```

## Pagination

Handle paginated REST APIs:

### Offset-Based Pagination

```json
{
 "sourcePath": "$.page.offset",
 "destinationPath": "__query.offset"
},
{
 "sourcePath": "$.page.limit",
 "destinationPath": "__query.limit"
}
```

**Request**: `GET /api/customers?offset=0&limit=100`

### Cursor-Based Pagination

```json
{
 "sourcePath": "$.pagination.cursor",
 "destinationPath": "__query.cursor"
}
```

**Request**: `GET /api/customers?cursor=eyJpZCI6MTIzfQ`

### Page-Based Pagination

```json
{
 "sourcePath": "$.page.number",
 "destinationPath": "__query.page"
},
{
 "sourcePath": "$.page.size",
 "destinationPath": "__query.size"
}
```

**Request**: `GET /api/customers?page=1&size=50`

## Best Practices

### 1. Use Descriptive Field Names

Map to clear, self-documenting JSON structure:

 **Good**:
```json
{
 "customer": {
 "contactInformation": {
 "primaryEmail": "john@example.com",
 "primaryPhone": "(555) 123-4567"
 }
 }
}
```

 **Bad**:
```json
{
 "c": {
 "ci": {
 "e1": "john@example.com",
 "p1": "(555) 123-4567"
 }
 }
}
```

### 2. Follow JSON Naming Conventions

Use camelCase for JSON properties (common convention):

```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe",
 "emailAddress": "john@example.com"
 }
}
```

### 3. Handle Null Values Explicitly

Decide how to handle nulls:

**Option 1: Omit null fields**
```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe"
 // email is omitted if null
 }
}
```

**Option 2: Include as null**
```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe",
 "email": null
 }
}
```

### 4. Validate JSON Structure

Use JSON Schema validation:

```json
{
 "validationConfig": {
 "inputSchema": "{...}",
 "outputSchema": "{...}"
 }
}
```

### 5. Use Appropriate HTTP Methods

- `POST` for creating resources
- `PUT` for full updates
- `PATCH` for partial updates
- `DELETE` for removal
- `GET` for retrieval (read-only)

### 6. Log Request/Response

Enable message capture to debug issues:

```json
{
 "enableMessageCapture": true
}
```

### 7. Set Timeouts

Configure appropriate timeouts:

```json
{
 "destinationConfig": {
 "timeoutMs": 30000
 }
}
```

## Troubleshooting

### 400 Bad Request

**Cause**: Invalid JSON or missing required fields

**Solutions**:
1. Validate output with JSON Schema
2. Check required fields are mapped
3. Verify data types are correct
4. Test with integration test mode

### 401 Unauthorized

**Cause**: Missing or invalid authentication

**Solutions**:
1. Verify authentication configuration
2. Check token/API key is valid
3. Ensure auth headers are sent
4. Test authentication separately

### 404 Not Found

**Cause**: Endpoint URL incorrect

**Solutions**:
1. Verify endpoint URL
2. Check URL parameters are correct
3. Ensure resource exists in destination system

### 429 Too Many Requests

**Cause**: Rate limit exceeded

**Solutions**:
1. Reduce request rate
2. Implement backoff and retry
3. Request higher rate limit from API provider

### JSON Parsing Errors

**Cause**: Invalid JSON format

**Solutions**:
1. Validate JSON with linter
2. Check for unescaped special characters
3. Ensure proper quoting of strings
4. Verify nested structure is correct

## Next Steps

- [SOAP/XML Protocol](soap-xml.md) - Working with SOAP services
- [gRPC Protocol](grpc.md) - Modern RPC with gRPC
- [Field Mappings](../field-mappings.md) - JSONPath reference
- [Authentication](../behaviors.md#authenticationbehavior) - Auth configuration
