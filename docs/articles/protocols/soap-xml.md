# SOAP/XML Protocol

SOAP (Simple Object Access Protocol) is a protocol for exchanging structured information in web services. This guide covers SOAP integration with QuickApiMapper.

## Overview

SOAP uses XML for message format and typically HTTP for transport. QuickApiMapper supports:

- **SOAP 1.1 and 1.2** - Both protocol versions
- **WSDL** - Automatic envelope generation from WSDL (planned)
- **WS-Security** - Security extensions
- **Custom Headers** - SOAP header elements

## SOAP Destination Configuration

### Basic Configuration

```json
{
 "name": "Customer to ERP SOAP",
 "destinationType": "SOAP",
 "soapConfig": {
 "endpointUrl": "https://ifs.example.com/services/CustomerService",
 "soapAction": "http://ifs.example.com/CreateCustomer",
 "methodName": "CreateCustomer",
 "targetNamespace": "http://ifs.example.com/types",
 "soapVersion": "1.1"
 }
}
```

### Configuration Properties

- **endpointUrl** - SOAP service endpoint URL
- **soapAction** - SOAPAction header value (required for SOAP 1.1)
- **methodName** - SOAP method/operation name
- **targetNamespace** - XML namespace for the request body
- **soapVersion** - "1.1" or "1.2" (default: "1.1")

## SOAP Envelope Structure

### SOAP 1.1 Envelope

```xml
<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
 xmlns:types="http://ifs.example.com/types">
 <soap:Header>
 <!-- Optional header elements -->
 </soap:Header>
 <soap:Body>
 <types:CreateCustomer>
 <!-- Your mapped data here -->
 <types:Customer>
 <types:FirstName>John</types:FirstName>
 <types:LastName>Doe</types:LastName>
 </types:Customer>
 </types:CreateCustomer>
 </soap:Body>
</soap:Envelope>
```

### SOAP 1.2 Envelope

```xml
<?xml version="1.0" encoding="UTF-8"?>
<env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope"
 xmlns:types="http://ifs.example.com/types">
 <env:Header>
 <!-- Optional header elements -->
 </env:Header>
 <env:Body>
 <types:CreateCustomer>
 <types:Customer>
 <types:FirstName>John</types:FirstName>
 <types:LastName>Doe</types:LastName>
 </types:Customer>
 </types:CreateCustomer>
 </env:Body>
</env:Envelope>
```

## Complete Example

### Scenario

Send customer data to ERP ERP system via SOAP.

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
 "name": "Customer to ERP",
 "integrationId": "customer-to-ifs",
 "sourceType": "JSON",
 "destinationType": "SOAP",
 "soapConfig": {
 "endpointUrl": "https://ifs.example.com/services/CustomerService",
 "soapAction": "http://ifs.example.com/CreateCustomer",
 "methodName": "CreateCustomer",
 "targetNamespace": "http://ifs.example.com/types"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.customer.customerId",
 "destinationPath": "Customer.@id"
 },
 {
 "sourcePath": "$.customer.firstName",
 "destinationPath": "Customer.FirstName"
 },
 {
 "sourcePath": "$.customer.lastName",
 "destinationPath": "Customer.LastName",
 "transformers": [{"transformerName": "ToUpper"}]
 },
 {
 "sourcePath": "$.customer.email",
 "destinationPath": "Customer.Email"
 },
 {
 "sourcePath": "$.customer.phone",
 "destinationPath": "Customer.Phone",
 "transformers": [{"transformerName": "FormatPhone"}]
 },
 {
 "sourcePath": "$.customer.address.street",
 "destinationPath": "Customer.Address.Street"
 },
 {
 "sourcePath": "$.customer.address.city",
 "destinationPath": "Customer.Address.City"
 },
 {
 "sourcePath": "$.customer.address.state",
 "destinationPath": "Customer.Address.State"
 },
 {
 "sourcePath": "$.customer.address.zip",
 "destinationPath": "Customer.Address.ZipCode"
 },
 {
 "sourcePath": "$.customer.isActive",
 "destinationPath": "Customer.IsActive",
 "transformers": [{"transformerName": "BooleanToYN"}]
 }
 ],
 "staticValues": [
 {
 "destinationPath": "Customer.Source",
 "staticValue": "API"
 }
 ]
}
```

### Generated SOAP Request

```xml
<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
 xmlns:types="http://ifs.example.com/types">
 <soap:Body>
 <types:CreateCustomer>
 <types:Customer id="C12345">
 <types:FirstName>John</types:FirstName>
 <types:LastName>DOE</types:LastName>
 <types:Email>john.doe@example.com</types:Email>
 <types:Phone>(555) 123-4567</types:Phone>
 <types:Address>
 <types:Street>123 Main St</types:Street>
 <types:City>Boston</types:City>
 <types:State>MA</types:State>
 <types:ZipCode>02101</types:ZipCode>
 </types:Address>
 <types:IsActive>Y</types:IsActive>
 <types:Source>API</types:Source>
 </types:Customer>
 </types:CreateCustomer>
 </soap:Body>
</soap:Envelope>
```

## SOAP Headers

### Adding Custom Headers

Configure SOAP header elements:

```json
{
 "soapConfig": {
 "headers": [
 {
 "name": "AuthenticationToken",
 "namespace": "http://ifs.example.com/security",
 "value": "abc123token"
 },
 {
 "name": "ClientId",
 "namespace": "http://ifs.example.com/client",
 "value": "quickapimapper"
 }
 ]
 }
}
```

**Generated**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
 xmlns:sec="http://ifs.example.com/security"
 xmlns:client="http://ifs.example.com/client">
 <soap:Header>
 <sec:AuthenticationToken>abc123token</sec:AuthenticationToken>
 <client:ClientId>quickapimapper</client:ClientId>
 </soap:Header>
 <soap:Body>
 <!-- Body content -->
 </soap:Body>
</soap:Envelope>
```

### WS-Security (Username Token)

```json
{
 "soapConfig": {
 "security": {
 "type": "UsernameToken",
 "username": "apiuser",
 "password": "password123"
 }
 }
}
```

**Generated**:
```xml
<soap:Header>
 <wsse:Security xmlns:wsse="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
 <wsse:UsernameToken>
 <wsse:Username>apiuser</wsse:Username>
 <wsse:Password Type="...#PasswordText">password123</wsse:Password>
 </wsse:UsernameToken>
 </wsse:Security>
</soap:Header>
```

## XML Namespaces

### Default Namespace

```json
{
 "soapConfig": {
 "targetNamespace": "http://ifs.example.com/types"
 }
}
```

### Multiple Namespaces

```json
{
 "soapConfig": {
 "namespaces": {
 "types": "http://ifs.example.com/types",
 "common": "http://ifs.example.com/common",
 "custom": "http://example.com/custom"
 }
 }
}
```

Use in field mappings:
```json
{
 "destinationPath": "common:Header.custom:ClientInfo.types:Name"
}
```

## XML Attributes

Use `@` prefix for attributes:

```json
{
 "sourcePath": "$.customer.id",
 "destinationPath": "Customer.@customerId"
}
```

**Output**:
```xml
<Customer customerId="C12345">
 <FirstName>John</FirstName>
</Customer>
```

## Handling SOAP Faults

SOAP services return faults for errors:

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
 <soap:Body>
 <soap:Fault>
 <faultcode>soap:Client</faultcode>
 <faultstring>Invalid customer ID</faultstring>
 <detail>
 <error xmlns="http://ifs.example.com/errors">
 <code>INVALID_ID</code>
 <message>Customer ID must be 10 characters</message>
 </error>
 </detail>
 </soap:Fault>
 </soap:Body>
</soap:Envelope>
```

QuickApiMapper automatically:
- Detects SOAP faults
- Extracts fault code and message
- Returns error in `MappingResult`

## Best Practices

### 1. Use Correct SOAP Version

Check the target service's WSDL to determine version:
- SOAP 1.1: `http://schemas.xmlsoap.org/wsdl/soap/`
- SOAP 1.2: `http://schemas.xmlsoap.org/wsdl/soap12/`

### 2. Properly Escape XML

QuickApiMapper automatically escapes:
- `<` → `&lt;`
- `>` → `&gt;`
- `&` → `&amp;`
- `"` → `&quot;`
- `'` → `&apos;`

### 3. Validate Against XSD

If the SOAP service provides an XSD schema, validate your output:

```json
{
 "validationConfig": {
 "outputSchema": "path/to/customer.xsd"
 }
}
```

### 4. Set Appropriate Timeouts

SOAP calls can be slow:

```json
{
 "soapConfig": {
 "timeoutMs": 60000
 }
}
```

### 5. Handle Large Payloads

For large messages, consider:
- MTOM (Message Transmission Optimization Mechanism)
- Compression
- Chunking

## Troubleshooting

### SOAP Fault: Invalid Namespace

**Error**: `Namespace mismatch`

**Solution**: Verify `targetNamespace` matches WSDL

### Authentication Failed

**Error**: `401 Unauthorized` or `Security validation failed`

**Solutions**:
1. Check WS-Security configuration
2. Verify username/password
3. Ensure security headers are correct

### Method Not Found

**Error**: `Method 'XYZ' not found`

**Solutions**:
1. Verify `methodName` matches WSDL operation
2. Check case sensitivity
3. Ensure namespace is correct

### Malformed XML

**Error**: `XML parsing error`

**Solutions**:
1. Test with integration test mode
2. Validate XML structure
3. Check for unescaped special characters
4. Verify all tags are properly closed

## Next Steps

- [REST/JSON Protocol](rest-json.md) - JSON-based integrations
- [gRPC Protocol](grpc.md) - Modern RPC
- [Field Mappings](../field-mappings.md) - XPath reference
