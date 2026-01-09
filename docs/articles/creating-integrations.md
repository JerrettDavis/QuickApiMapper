# Creating Integrations

This guide walks you through creating integrations in QuickApiMapper, from basic configurations to advanced scenarios.

## Overview

An integration in QuickApiMapper consists of:

1. **Basic Configuration** - Name, ID, source/destination types
2. **Field Mappings** - How source fields map to destination fields
3. **Transformers** - Data transformations applied to fields
4. **Static Values** - Fixed values always included in output
5. **Protocol Configuration** - Protocol-specific settings (SOAP, gRPC, etc.)

## Creating an Integration via Web Designer

### Step 1: Navigate to Create Integration

1. Open the Web Designer at `http://localhost:5173`
2. Click "Integrations" in the navigation menu
3. Click the "Create Integration" button

### Step 2: Configure Basic Information

Fill in the basic integration details:

- **Name**: Descriptive name (e.g., "Customer to ERP Integration")
- **Integration ID**: URL-friendly identifier (auto-generated from name)
 - Must be unique
 - Used in API endpoints: `/api/map/{integrationId}`
 - Cannot be changed after creation
- **Description**: Optional description of the integration's purpose

### Step 3: Select Source and Destination Types

Choose the source and destination formats:

**Source Types**:
- **JSON** - RESTful APIs, webhooks
- **XML** - Legacy systems, file imports
- **gRPC** - Modern microservices

**Destination Types**:
- **JSON** - RESTful APIs
- **SOAP** - Enterprise systems (SAP, Oracle, etc.)
- **gRPC** - Modern microservices
- **RabbitMQ** - Message queues
- **Azure Service Bus** - Cloud messaging

### Step 4: Configure Destination Settings

Based on your destination type, configure protocol-specific settings:

#### SOAP Configuration

- **Endpoint URL**: SOAP service endpoint (e.g., `https://api.example.com/soap`)
- **SOAP Action**: SOAPAction header value (e.g., `http://example.com/CreateCustomer`)
- **Method Name**: SOAP method to invoke (e.g., `CreateCustomer`)
- **Target Namespace**: XML namespace for the request (optional)

**SOAP Fields** (optional):
Add custom SOAP header fields if required by the destination service.

#### gRPC Configuration

- **Endpoint URL**: gRPC server address (e.g., `https://grpc.example.com:5001`)
- **Service Name**: Full service name (e.g., `customers.v1.CustomerService`)
- **Method Name**: RPC method name (e.g., `CreateCustomer`)
- **Proto File Path**: Path to .proto definition (optional)

#### RabbitMQ Configuration

- **Connection String**: RabbitMQ connection (e.g., `amqp://localhost`)
- **Exchange**: Exchange name (e.g., `customer-events`)
- **Routing Key**: Routing key (e.g., `customer.created`)
- **Queue Name**: Queue name if using direct queue (optional)

#### Service Bus Configuration

- **Connection String**: Azure Service Bus connection string
- **Queue Name** or **Topic Name**: Destination queue/topic
- **Subscription Name**: If using topics (optional)

### Step 5: Add Field Mappings

Field mappings define how data transforms from source to destination.

Click "Add Mapping" to add each field:

#### Source Path

Use path notation to reference source fields:

**JSON (JSONPath)**:
- `$.customer.firstName` - Simple property
- `$.customers[0].name` - Array index
- `$.order.items[*].price` - All items
- `$..email` - Recursive descent

**XML (XPath)**:
- `/Customer/FirstName` - Simple element
- `//Customer/@id` - Attribute
- `/Order/Items/Item[1]/Price` - Array index
- `//Customer/Email/text()` - Text content

#### Destination Path

Use dot notation for the destination structure:

- `Customer.FirstName` - Simple property
- `Order.Items.Item.Price` - Nested structure
- `@id` - Attribute (XML only)

#### Example Mappings

| Source Path | Destination Path | Description |
|-------------|------------------|-------------|
| `$.customer.firstName` | `Customer.FirstName` | Simple mapping |
| `$.customer.lastName` | `Customer.LastName` | Simple mapping |
| `$.customer.email` | `Customer.ContactInfo.Email` | Nested destination |
| `$.customer.phone` | `Customer.ContactInfo.Phone` | Nested destination |
| `$.order.total` | `Order.TotalAmount` | Simple mapping |

### Step 6: Add Transformers

Transformers modify field values during mapping. You can chain multiple transformers.

**Built-in Transformers**:

- **ToUpper** - Converts to uppercase
- **ToLower** - Converts to lowercase
- **Trim** - Removes whitespace
- **FormatPhone** - Formats phone numbers `(555) 123-4567`
- **ToBool** - Converts to boolean
- **BoolToYN** - Converts boolean to Y/N
- **FormatDate** - Formats dates

**Adding Transformers**:

1. Click "Add Transformer" on a field mapping
2. Select transformer from dropdown
3. Configure parameters if required
4. Transformers execute in order added

**Example**:
```
Source: " john DOE "
Transformers: Trim → ToLower
Result: "john doe"
```

### Step 7: Add Static Values

Static values are constant values always included in the output, regardless of input.

Click "Add Static Value":

- **Destination Path**: Where to place the value (e.g., `Customer.Source`)
- **Static Value**: The constant value (e.g., `"API"`)

**Use Cases**:
- Source system identifiers
- Environment markers (DEV, PROD)
- Default values
- Fixed timestamps

**Example**:
```xml
<Customer>
 <FirstName>John</FirstName>
 <Source>API</Source> <!-- Static value -->
 <Environment>PROD</Environment> <!-- Static value -->
</Customer>
```

### Step 8: Configure Behaviors (Optional)

Behaviors add cross-cutting concerns to your integration.

**Available Behaviors**:

#### Authentication Behavior
Adds authentication to outbound requests.

**Configuration**:
- Auth Type: Bearer, ApiKey, Basic
- Token/Key: Credential value
- Header Name: Custom header name

#### Validation Behavior
Validates input/output against schemas.

**Configuration**:
- Input Schema: JSON Schema for validation
- Output Schema: JSON Schema for validation
- Fail on Validation Error: true/false

#### Timing Behavior
Measures and logs execution time.

**Configuration**:
- Enabled: true/false
- Log Threshold: Minimum duration to log (ms)

### Step 9: Test the Integration

Before saving, test your integration with sample data.

Click "Test Integration":

1. **Input Tab**: Paste sample JSON/XML payload
2. Click "Test Transform"
3. **Output Tab**: View transformed output
4. **Debug Tab**: See field-by-field transformations

**Sample Test Payload (JSON)**:
```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "isActive": true
 }
}
```

Review the output to ensure mappings are correct.

### Step 10: Save the Integration

Click "Save Integration" to persist to the database.

The integration is now available at: `POST /api/map/{integrationId}`

## Creating an Integration via API

You can also create integrations programmatically via the Management API.

### Endpoint

```
POST /api/integrations
Content-Type: application/json
```

### Request Body

```json
{
 "name": "Customer to ERP Integration",
 "integrationId": "customer-to-ifs",
 "description": "Syncs customer data to ERP",
 "sourceType": "JSON",
 "destinationType": "SOAP",
 "soapConfig": {
 "endpointUrl": "https://ifs.example.com/soap",
 "soapAction": "http://ifs.example.com/CreateCustomer",
 "methodName": "CreateCustomer",
 "targetNamespace": "http://ifs.example.com/types"
 },
 "fieldMappings": [
 {
 "sourcePath": "$.customer.firstName",
 "destinationPath": "Customer.FirstName",
 "transformers": []
 },
 {
 "sourcePath": "$.customer.lastName",
 "destinationPath": "Customer.LastName",
 "transformers": [
 {
 "transformerName": "ToUpper",
 "parameters": {}
 }
 ]
 },
 {
 "sourcePath": "$.customer.phone",
 "destinationPath": "Customer.Phone",
 "transformers": [
 {
 "transformerName": "FormatPhone",
 "parameters": {}
 }
 ]
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

### Response

```json
{
 "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
 "integrationId": "customer-to-ifs",
 "name": "Customer to ERP Integration",
 "createdAt": "2026-01-08T10:00:00Z"
}
```

## Advanced Scenarios

### Complex Field Mappings

#### Mapping Arrays

**Source (JSON)**:
```json
{
 "order": {
 "items": [
 {"sku": "ABC123", "quantity": 2},
 {"sku": "XYZ789", "quantity": 1}
 ]
 }
}
```

**Mappings**:
- `$.order.items[0].sku` → `Order.Items.Item[0].SKU`
- `$.order.items[0].quantity` → `Order.Items.Item[0].Quantity`
- `$.order.items[1].sku` → `Order.Items.Item[1].SKU`
- `$.order.items[1].quantity` → `Order.Items.Item[1].Quantity`

**Output (XML)**:
```xml
<Order>
 <Items>
 <Item>
 <SKU>ABC123</SKU>
 <Quantity>2</Quantity>
 </Item>
 <Item>
 <SKU>XYZ789</SKU>
 <Quantity>1</Quantity>
 </Item>
 </Items>
</Order>
```

#### Conditional Mappings

Use transformers with conditions:

```json
{
 "sourcePath": "$.customer.type",
 "destinationPath": "Customer.TypeCode",
 "transformers": [
 {
 "transformerName": "MapValue",
 "parameters": {
 "individual": "I",
 "business": "B",
 "government": "G"
 }
 }
 ]
}
```

#### Combining Fields

Use a custom transformer:

```csharp
public class CombineNameTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 var firstName = context.SourceData["$.customer.firstName"];
 var lastName = context.SourceData["$.customer.lastName"];
 return $"{firstName} {lastName}";
 }
}
```

### Protocol-Specific Examples

#### gRPC Integration

```json
{
 "name": "Order to Fulfillment gRPC",
 "sourceType": "JSON",
 "destinationType": "gRPC",
 "grpcConfig": {
 "endpointUrl": "https://fulfillment.example.com:5001",
 "serviceName": "fulfillment.v1.OrderService",
 "methodName": "CreateOrder"
 },
 "fieldMappings": [...]
}
```

#### RabbitMQ Integration

```json
{
 "name": "Customer Events to RabbitMQ",
 "sourceType": "JSON",
 "destinationType": "RabbitMQ",
 "rabbitMqConfig": {
 "connectionString": "amqp://rabbitmq:5672",
 "exchange": "customer-events",
 "routingKey": "customer.created"
 },
 "fieldMappings": [...]
}
```

## Best Practices

### 1. Naming Conventions

- **Integration Name**: Descriptive, include source and destination
 - Good: "Salesforce Customer to ERP Integration"
 - Bad: "Integration1"

- **Integration ID**: Lowercase, kebab-case
 - Good: `salesforce-customer-to-ifs`
 - Bad: `SalesforceCustomer_To_ERP`

### 2. Error Handling

- Enable validation behaviors for production integrations
- Configure retry logic for transient failures
- Set up monitoring and alerts

### 3. Testing

- Always test with real sample data
- Test edge cases (null values, empty arrays, special characters)
- Test with the debug tab to verify field-by-field transformations

### 4. Security

- Never hardcode credentials in integration configs
- Use environment variables or secret management
- Enable authentication behaviors
- Use HTTPS for all endpoints

### 5. Performance

- Minimize the number of transformers per field
- Cache integration configs with `CachedConfigurationProvider`
- Use async behaviors for external calls

### 6. Documentation

- Add clear descriptions to integrations
- Document expected input/output formats
- Keep sample payloads for testing

## Troubleshooting

### Integration Not Found

**Error**: `Integration 'xyz' not found`

**Solutions**:
- Verify integration ID is correct (case-sensitive)
- Check integration exists in database
- Ensure integration is enabled

### Source Path Not Resolved

**Error**: `Failed to resolve source path '$.customer.name'`

**Solutions**:
- Verify JSONPath/XPath syntax
- Check source payload structure
- Test path with sample payload in test mode

### Transformation Failed

**Error**: `Transformer 'ToUpper' failed`

**Solutions**:
- Ensure transformer is registered
- Check transformer parameters
- Verify input value type matches transformer expectations

### Destination Call Failed

**Error**: `Failed to call destination endpoint`

**Solutions**:
- Verify endpoint URL is reachable
- Check authentication configuration
- Review destination service logs
- Enable timing behavior to see request/response details

## Next Steps

- [Field Mappings](field-mappings.md) - Deep dive into field mapping syntax
- [Transformers](transformers.md) - Learn about all available transformers
- [Behaviors](behaviors.md) - Add authentication, validation, and more
- [Message Capture](message-capture.md) - Debug integrations with message capture
- [Deployment](deployment.md) - Deploy integrations to production
