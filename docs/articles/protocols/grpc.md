# gRPC Protocol

gRPC is a modern, high-performance RPC framework. This guide covers gRPC integration with QuickApiMapper.

## Overview

gRPC uses Protocol Buffers (protobuf) for serialization and HTTP/2 for transport. QuickApiMapper supports:

- **Unary RPCs** - Single request, single response
- **Protocol Buffers** - Automatic serialization/deserialization
- **Strong Typing** - Schema-based contracts
- **HTTP/2** - Multiplexing and streaming

## gRPC Configuration

### Basic Setup

```json
{
 "name": "Customer to gRPC Service",
 "destinationType": "gRPC",
 "grpcConfig": {
 "endpointUrl": "https://grpc.example.com:5001",
 "serviceName": "customers.v1.CustomerService",
 "methodName": "CreateCustomer",
 "protoFile": "path/to/customer.proto"
 }
}
```

### Configuration Properties

- **endpointUrl** - gRPC server address (with port)
- **serviceName** - Fully qualified service name
- **methodName** - RPC method to invoke
- **protoFile** - Path to .proto file (optional)
- **useTls** - Enable TLS (default: true)
- **maxMessageSize** - Max message size in bytes

## Protocol Buffer Definition

### Example .proto File

```protobuf
syntax = "proto3";

package customers.v1;

service CustomerService {
 rpc CreateCustomer(CreateCustomerRequest) returns (CreateCustomerResponse);
 rpc GetCustomer(GetCustomerRequest) returns (Customer);
 rpc UpdateCustomer(UpdateCustomerRequest) returns (Customer);
}

message CreateCustomerRequest {
 Customer customer = 1;
}

message CreateCustomerResponse {
 string customer_id = 1;
 bool success = 2;
 string message = 3;
}

message Customer {
 string customer_id = 1;
 string first_name = 2;
 string last_name = 3;
 string email = 4;
 string phone = 5;
 Address address = 6;
 bool is_active = 7;
}

message Address {
 string street = 1;
 string city = 2;
 string state = 3;
 string zip_code = 4;
}

message GetCustomerRequest {
 string customer_id = 1;
}

message UpdateCustomerRequest {
 Customer customer = 1;
}
```

## Complete Example

### Scenario

Send customer data to a gRPC microservice.

### Source (JSON)

```json
{
 "customer": {
 "customerId": "C12345",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "address": {
 "street": "123 Main St",
 "city": "Boston",
 "state": "MA",
 "zip": "02101"
 },
 "isActive": true
 }
}
```

### Integration Configuration

```json
{
 "name": "Customer to gRPC",
 "integrationId": "customer-to-grpc",
 "sourceType": "JSON",
 "destinationType": "gRPC",
 "grpcConfig": {
 "endpointUrl": "https://customers-api.example.com:5001",
 "serviceName": "customers.v1.CustomerService",
 "methodName": "CreateCustomer"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.customer.customerId",
 "destinationPath": "customer.customer_id"
 },
 {
 "sourcePath": "$.customer.firstName",
 "destinationPath": "customer.first_name"
 },
 {
 "sourcePath": "$.customer.lastName",
 "destinationPath": "customer.last_name"
 },
 {
 "sourcePath": "$.customer.email",
 "destinationPath": "customer.email"
 },
 {
 "sourcePath": "$.customer.phone",
 "destinationPath": "customer.phone"
 },
 {
 "sourcePath": "$.customer.address.street",
 "destinationPath": "customer.address.street"
 },
 {
 "sourcePath": "$.customer.address.city",
 "destinationPath": "customer.address.city"
 },
 {
 "sourcePath": "$.customer.address.state",
 "destinationPath": "customer.address.state"
 },
 {
 "sourcePath": "$.customer.address.zip",
 "destinationPath": "customer.address.zip_code"
 },
 {
 "sourcePath": "$.customer.isActive",
 "destinationPath": "customer.is_active"
 }
 ]
}
```

### Generated gRPC Message

The integration creates a `CreateCustomerRequest` message:

```json
{
 "customer": {
 "customer_id": "C12345",
 "first_name": "John",
 "last_name": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "address": {
 "street": "123 Main St",
 "city": "Boston",
 "state": "MA",
 "zip_code": "02101"
 },
 "is_active": true
 }
}
```

This is serialized to protobuf binary format and sent via gRPC.

## Field Naming Conventions

Protocol Buffers use `snake_case`, while JSON typically uses `camelCase`.

### Mapping Conventions

**Source (JSON)**: camelCase
```json
{
 "firstName": "John",
 "lastName": "Doe",
 "isActive": true
}
```

**Destination (gRPC)**: snake_case
```protobuf
message Customer {
 string first_name = 1;
 string last_name = 2;
 bool is_active = 3;
}
```

**Mappings**:
- `$.firstName` → `first_name`
- `$.lastName` → `last_name`
- `$.isActive` → `is_active`

## Data Types

### Protobuf to C# Mapping

| Protobuf Type | C# Type | JSON Type |
|---------------|---------|-----------|
| double | double | number |
| float | float | number |
| int32 | int | number |
| int64 | long | string |
| uint32 | uint | number |
| uint64 | ulong | string |
| sint32 | int | number |
| sint64 | long | string |
| fixed32 | uint | number |
| fixed64 | ulong | string |
| bool | bool | boolean |
| string | string | string |
| bytes | ByteString | base64 string |

### Handling Type Conversions

Use transformers for type conversions:

```json
{
 "sourcePath": "$.price",
 "destinationPath": "product.price",
 "transformers": [
 {"transformerName": "ToDouble"}
 ]
}
```

## Authentication

### TLS Client Certificates

```json
{
 "grpcConfig": {
 "useTls": true,
 "clientCertPath": "/path/to/client.crt",
 "clientKeyPath": "/path/to/client.key",
 "caCertPath": "/path/to/ca.crt"
 }
}
```

### Metadata (Headers)

Add authentication via gRPC metadata:

```json
{
 "authenticationConfig": {
 "type": "Bearer",
 "token": "your-token-here"
 }
}
```

This adds metadata:
```
authorization: Bearer your-token-here
```

### API Keys

```json
{
 "grpcConfig": {
 "metadata": {
 "x-api-key": "your-api-key",
 "x-client-id": "quickapimapper"
 }
 }
}
```

## Error Handling

### gRPC Status Codes

gRPC uses specific status codes:

| Code | Name | Description |
|------|------|-------------|
| 0 | OK | Success |
| 1 | CANCELLED | Operation cancelled |
| 2 | UNKNOWN | Unknown error |
| 3 | INVALID_ARGUMENT | Invalid argument |
| 4 | DEADLINE_EXCEEDED | Timeout |
| 5 | NOT_FOUND | Resource not found |
| 6 | ALREADY_EXISTS | Resource exists |
| 7 | PERMISSION_DENIED | Permission denied |
| 8 | RESOURCE_EXHAUSTED | Rate limited |
| 9 | FAILED_PRECONDITION | Precondition failed |
| 14 | UNAVAILABLE | Service unavailable |
| 16 | UNAUTHENTICATED | Not authenticated |

### Retry Configuration

```json
{
 "grpcConfig": {
 "retryPolicy": {
 "maxRetries": 3,
 "retryOn": ["UNAVAILABLE", "DEADLINE_EXCEEDED"],
 "backoffMs": [100, 200, 400]
 }
 }
}
```

### Timeout Configuration

```json
{
 "grpcConfig": {
 "timeoutMs": 5000
 }
}
```

## Advanced Features

### Compression

Enable compression for large messages:

```json
{
 "grpcConfig": {
 "compression": "gzip"
 }
}
```

### Keep-Alive

Configure keep-alive pings:

```json
{
 "grpcConfig": {
 "keepAliveTimeMs": 30000,
 "keepAliveTimeoutMs": 10000
 }
}
```

### Max Message Size

Increase for large payloads:

```json
{
 "grpcConfig": {
 "maxSendMessageSize": 10485760,
 "maxReceiveMessageSize": 10485760
 }
}
```

## Best Practices

### 1. Use Versioned Services

Include version in service name:

```protobuf
package customers.v1;

service CustomerService {
 // RPCs
}
```

### 2. Define Clear Contracts

Use descriptive message and field names:

```protobuf
message CreateCustomerRequest {
 Customer customer = 1;
}

// NOT: message CreateReq { C c = 1; }
```

### 3. Handle Backwards Compatibility

- Never change field numbers
- Use `reserved` for deprecated fields
- Add new fields with higher numbers

```protobuf
message Customer {
 reserved 4; // Deprecated field
 reserved "old_field_name";

 string customer_id = 1;
 string first_name = 2;
 string last_name = 3;
 // Field 4 is reserved
 string email = 5; // New field
}
```

### 4. Use Well-Known Types

Leverage protobuf well-known types:

```protobuf
import "google/protobuf/timestamp.proto";
import "google/protobuf/duration.proto";
import "google/protobuf/empty.proto";

message Order {
 google.protobuf.Timestamp created_at = 1;
 google.protobuf.Duration processing_time = 2;
}
```

### 5. Enable Health Checks

Implement gRPC health checking:

```json
{
 "grpcConfig": {
 "enableHealthCheck": true
 }
}
```

## Troubleshooting

### Connection Refused

**Error**: `UNAVAILABLE: Connection refused`

**Solutions**:
1. Verify server is running
2. Check endpoint URL and port
3. Ensure firewall allows traffic
4. Verify TLS configuration

### Invalid Argument

**Error**: `INVALID_ARGUMENT: Invalid field value`

**Solutions**:
1. Check field types match protobuf definition
2. Verify required fields are provided
3. Validate data ranges
4. Test with integration test mode

### Deadline Exceeded

**Error**: `DEADLINE_EXCEEDED: Timeout`

**Solutions**:
1. Increase timeout
2. Check server performance
3. Verify network connectivity
4. Enable retry with backoff

### Unauthenticated

**Error**: `UNAUTHENTICATED: No valid credentials`

**Solutions**:
1. Verify authentication token
2. Check metadata headers
3. Ensure TLS certificates are valid
4. Test authentication separately

## Comparison with REST

| Feature | gRPC | REST |
|---------|------|------|
| Protocol | HTTP/2 | HTTP/1.1 |
| Format | Protobuf (binary) | JSON (text) |
| Schema | Required (.proto) | Optional (OpenAPI) |
| Performance | Fast | Slower |
| Streaming | Yes | Limited |
| Browser Support | Limited | Full |
| Human Readable | No | Yes |

**When to Use gRPC**:
- High performance requirements
- Microservice communication
- Strong typing needed
- Streaming data

**When to Use REST**:
- Browser clients
- Public APIs
- Human readability important
- Simple CRUD operations

## Next Steps

- [REST/JSON Protocol](rest-json.md) - JSON-based integrations
- [SOAP/XML Protocol](soap-xml.md) - SOAP services
- [Creating Integrations](../creating-integrations.md) - Build integrations
