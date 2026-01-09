# QuickApiMapper.Extensions.gRPC

gRPC protocol extension for QuickApiMapper, enabling integration mappings between gRPC (Protobuf) and other data formats (JSON, XML, SOAP).

## Features

- **Bidirectional Mapping**: Map between gRPC messages and JSON/XML/SOAP
- **Dynamic Field Resolution**: Extract values from Protobuf messages using path expressions
- **Type-Safe Conversion**: Automatic type conversion between Protobuf scalar types and strings
- **Nested Message Support**: Navigate and map nested message structures
- **Repeated Field Handling**: Access array elements with index notation
- **Enum Support**: Parse enum values by name or number
- **gRPC Client Integration**: Forward requests to downstream gRPC services
- **HTTP/2 Support**: Full HTTP/2 protocol support for gRPC communication

## Installation

Add the package reference to your project:

```xml
<PackageReference Include="QuickApiMapper.Extensions.gRPC" />
```

Register gRPC support in your application:

```csharp
builder.Services.AddQuickApiMapper();
builder.Services.AddGrpcSupport(options =>
{
 options.EnableReflection = true; // For development/testing
 options.MaxMessageSize = 4 * 1024 * 1024; // 4 MB
 options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
```

## Field Path Syntax

The gRPC source resolver supports a path syntax similar to JSONPath:

- **Simple field**: `customerName`
- **Nested field**: `customer.address.city`
- **Repeated field (array)**: `items[0].productId`
- **Static values**: `$static:ApiKey`

### Examples

```
// Top-level field
"userId" → message.userId

// Nested message
"customer.name" → message.customer.name

// First item in repeated field
"orders[0].total" → message.orders[0].total

// Static value reference
"$static:TenantId" → "tenant-123" (from configuration)
```

## Usage Examples

### Example 1: JSON to gRPC

Map JSON input to a gRPC message and forward to a downstream service.

**Configuration** (appsettings.json):

```json
{
 "ApiMapping": {
 "Mappings": [
 {
 "Name": "CreateUserGrpc",
 "Endpoint": "/api/users/create",
 "SourceType": "JSON",
 "DestinationType": "gRPC",
 "DestinationUrl": "https://grpc-service:5001/users.UserService/CreateUser",
 "Mapping": [
 {
 "Source": "$.name",
 "Destination": "name"
 },
 {
 "Source": "$.email",
 "Destination": "email"
 },
 {
 "Source": "$.age",
 "Destination": "age"
 }
 ],
 "GrpcConfig": {
 "ServiceName": "users.UserService",
 "MethodName": "CreateUser",
 "MessageType": "CreateUserRequest"
 }
 }
 ]
 }
}
```

**Request**:

```bash
curl -X POST http://localhost:5000/api/users/create \
 -H "Content-Type: application/json" \
 -d '{
 "name": "John Doe",
 "email": "john@example.com",
 "age": 30
 }'
```

**Result**: JSON is mapped to a `CreateUserRequest` Protobuf message and sent to the gRPC service.

### Example 2: gRPC to JSON

Receive gRPC requests and transform them to JSON for a REST API.

**Proto Definition** (user.proto):

```protobuf
syntax = "proto3";

package users;

service UserService {
 rpc GetUser(GetUserRequest) returns (GetUserResponse);
}

message GetUserRequest {
 string user_id = 1;
}

message GetUserResponse {
 string user_id = 1;
 string name = 2;
 string email = 3;
 int32 age = 4;
 Address address = 5;
}

message Address {
 string street = 1;
 string city = 2;
 string state = 3;
 string zip_code = 4;
}
```

**Configuration**:

```json
{
 "Name": "GetUserRest",
 "SourceType": "gRPC",
 "DestinationType": "JSON",
 "DestinationUrl": "https://rest-api/users/{userId}",
 "Mapping": [
 {
 "Source": "userId",
 "Destination": "$.id"
 },
 {
 "Source": "name",
 "Destination": "$.fullName"
 },
 {
 "Source": "address.city",
 "Destination": "$.location.city"
 }
 ]
}
```

### Example 3: gRPC to SOAP

Bridge gRPC microservices with legacy SOAP systems.

```json
{
 "Name": "OrderToLegacySystem",
 "SourceType": "gRPC",
 "DestinationType": "SOAP",
 "DestinationUrl": "https://legacy-soap-service/orders",
 "Mapping": [
 {
 "Source": "order.orderId",
 "Destination": "//OrderId"
 },
 {
 "Source": "order.items[0].productCode",
 "Destination": "//Product/Code"
 },
 {
 "Source": "order.total",
 "Destination": "//TotalAmount",
 "Transformers": [
 {
 "Name": "FormatCurrency",
 "Args": {
 "decimals": "2"
 }
 }
 ]
 }
 ]
}
```

## Protobuf Type Mapping

The extension automatically converts between Protobuf scalar types and strings:

| Protobuf Type | .NET Type | Example |
|---------------|-----------|---------|
| `double` | `double` | `3.14159` |
| `float` | `float` | `3.14f` |
| `int32` | `int` | `42` |
| `int64` | `long` | `9223372036854775807` |
| `uint32` | `uint` | `4294967295` |
| `uint64` | `ulong` | `18446744073709551615` |
| `bool` | `bool` | `true` |
| `string` | `string` | `"Hello"` |
| `bytes` | `byte[]` | `[0x48, 0x65, 0x6C, 0x6C, 0x6F]` |
| `enum` | `int` | `ACTIVE` or `1` |

## Downstream gRPC Calls

When `DestinationType` is `gRPC`, the handler:

1. Serializes the mapped Protobuf message to bytes
2. Creates an HTTP/2 request with gRPC headers
3. Sends the request to the `DestinationUrl`
4. Returns the gRPC response to the caller

**Headers set automatically:**
- `Content-Type: application/grpc`
- `grpc-encoding: identity`
- `grpc-accept-encoding: identity,gzip`

## Error Handling

gRPC status codes are mapped to HTTP status codes:

| gRPC Status | HTTP Status |
|-------------|-------------|
| `OK` | `200 OK` |
| `CANCELLED` | `499 Client Closed Request` |
| `INVALID_ARGUMENT` | `400 Bad Request` |
| `NOT_FOUND` | `404 Not Found` |
| `ALREADY_EXISTS` | `409 Conflict` |
| `PERMISSION_DENIED` | `403 Forbidden` |
| `UNAUTHENTICATED` | `401 Unauthorized` |
| `RESOURCE_EXHAUSTED` | `429 Too Many Requests` |
| `UNIMPLEMENTED` | `501 Not Implemented` |
| `UNAVAILABLE` | `503 Service Unavailable` |
| `DEADLINE_EXCEEDED` | `504 Gateway Timeout` |
| `INTERNAL` | `500 Internal Server Error` |

## Performance Considerations

- **HTTP/2 Multiplexing**: Supports multiple concurrent gRPC calls over a single connection
- **Connection Pooling**: Reuses connections with configurable idle timeout (default: 5 minutes)
- **Keep-Alive**: Ping interval of 60 seconds to keep connections alive
- **Message Size Limits**: Default 4 MB, configurable via `MaxMessageSize`

## Limitations (Current Version)

- **Unary Calls Only**: Server streaming, client streaming, and bidirectional streaming not yet supported
- **Static Message Types**: Message types must be known at compile time (no dynamic .proto parsing yet)
- **No Service Discovery**: Must specify exact gRPC endpoint URLs
- **Limited Reflection**: Proto file parsing for dynamic schemas planned for future release

## Future Enhancements

- [ ] Server streaming support
- [ ] Client streaming support
- [ ] Bidirectional streaming support
- [ ] Dynamic .proto file parsing and schema generation
- [ ] gRPC service discovery integration
- [ ] Interceptor support for authentication/logging
- [ ] Load balancing for downstream gRPC services
- [ ] Circuit breaker pattern for resilience

## See Also

- [gRPC Documentation](https://grpc.io/docs/)
- [Protocol Buffers Guide](https://developers.google.com/protocol-buffers)
- [QuickApiMapper Core Documentation](../README.md)
- [Integration Testing Guide](../docs/testing/grpc-integration-tests.md)
