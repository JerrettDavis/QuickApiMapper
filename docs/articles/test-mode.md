# Test Mode

Test mode allows you to preview transformations without sending data to destination systems. This guide covers using test mode effectively for development, debugging, and validation.

## Overview

Test mode executes the full mapping pipeline but stops before calling the destination handler. This lets you:

- Preview transformations with sample data
- Debug field mappings and transformers
- Validate integrations before deployment
- Test with various input scenarios
- Share transformation examples with stakeholders

## How Test Mode Works

```
┌────────────────────────────────────────────────────┐
│ Test Mode Execution │
└────────────────────────────────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Parse Sample Input │
 │ (JSON/XML/gRPC) │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Execute PreMap │
 │ Behaviors │
 │ (without external │
 │ calls) │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Apply Field Mappings │
 │ ├─ Resolve source │
 │ ├─ Apply transformers │
 │ └─ Map to destination │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Add Static Values │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Generate Output │
 │ (JSON/XML/gRPC) │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ SKIP Destination │
 │ Handler │
 │ (SOAP/gRPC/etc.) │
 └─────────────────────────┘
 │
 ▼
 ┌─────────────────────────┐
 │ Return Test Result │
 │ ├─ Transformed output │
 │ ├─ Transformation steps│
 │ ├─ Duration │
 │ └─ Errors (if any) │
 └─────────────────────────┘
```

## Using Test Mode

### Via Web Designer

#### Step 1: Open Test Dialog

1. Navigate to `http://localhost:5173`
2. Go to "Integrations"
3. Click on an integration
4. Click "Test Integration" button

Or from the integration list:
- Click the play icon (▶) on any integration row

#### Step 2: Provide Sample Input

In the **Input** tab:

1. Paste or type sample JSON/XML payload
2. Optionally override static values
3. Click "Test Transform"

**Example JSON Input**:
```json
{
 "customer": {
 "firstName": "Jane",
 "lastName": "Smith",
 "email": "jane.smith@example.com",
 "phone": "5559876543",
 "isActive": true
 }
}
```

#### Step 3: Review Output

Switch to the **Output** tab to see the transformed result:

**Example XML Output**:
```xml
<Customer>
 <FirstName>Jane</FirstName>
 <LastName>SMITH</LastName>
 <Email>jane.smith@example.com</Email>
 <Phone>(555) 987-6543</Phone>
 <Status>Y</Status>
 <Source>API</Source>
</Customer>
```

**Actions**:
- **Copy** - Copy output to clipboard
- **Download** - Save output as file
- **Retest** - Run test again with same or modified input

#### Step 4: Debug Transformations

Switch to the **Debug** tab to see field-by-field transformations:

| Field | Source Value | Transformers Applied | Destination Value |
|-------|--------------|---------------------|-------------------|
| FirstName | "Jane" | (none) | "Jane" |
| LastName | "Smith" | ToUpper | "SMITH" |
| Email | "jane.smith@example.com" | (none) | "jane.smith@example.com" |
| Phone | "5559876543" | FormatPhone | "(555) 987-6543" |
| Status | true | BooleanToYN | "Y" |

This helps identify:
- Which transformer is applied to each field
- Input vs output values
- Where transformations fail or produce unexpected results

### Via Management API

Test integrations programmatically using the API.

**Endpoint**:
```http
POST /api/integrations/{id}/test
Content-Type: application/json
```

**Request Body**:
```json
{
 "samplePayload": "{\"customer\": {\"firstName\": \"Jane\"}}",
 "overrideStaticValues": {
 "Customer.Source": "TEST"
 }
}
```

**Response**:
```json
{
 "success": true,
 "transformedPayload": "<Customer><FirstName>Jane</FirstName>...</Customer>",
 "errors": null,
 "metadata": {
 "durationMs": 45,
 "fieldsProcessed": 8,
 "transformersApplied": 3
 },
 "steps": [
 {
 "fieldPath": "Customer.FirstName",
 "sourceValue": "Jane",
 "transformedValue": "Jane",
 "transformersApplied": []
 },
 {
 "fieldPath": "Customer.LastName",
 "sourceValue": "Smith",
 "transformedValue": "SMITH",
 "transformersApplied": ["ToUpper"]
 }
 ]
}
```

**Error Response**:
```json
{
 "success": false,
 "transformedPayload": null,
 "errors": "Failed to resolve source path '$.customer.name': Path not found in source data",
 "metadata": {
 "durationMs": 12,
 "fieldsProcessed": 0,
 "transformersApplied": 0
 },
 "steps": []
}
```

## Test Scenarios

### Basic Functionality Test

Verify the integration works with valid data:

**Input**:
```json
{
 "customer": {
 "id": "C12345",
 "name": "John Doe",
 "email": "john@example.com"
 }
}
```

**Expected Output**:
```xml
<Customer>
 <CustomerId>C12345</CustomerId>
 <Name>John Doe</Name>
 <Email>john@example.com</Email>
</Customer>
```

### Edge Cases

#### Empty Values

**Input**:
```json
{
 "customer": {
 "id": "C12345",
 "name": "",
 "email": ""
 }
}
```

**Expected**: Empty values should be handled gracefully (not cause errors).

#### Null Values

**Input**:
```json
{
 "customer": {
 "id": "C12345",
 "name": null,
 "email": null
 }
}
```

**Expected**: Null values should either be skipped or converted to empty strings.

#### Missing Fields

**Input**:
```json
{
 "customer": {
 "id": "C12345"
 }
}
```

**Expected**: Missing optional fields should not cause errors. Required fields should be validated.

#### Special Characters

**Input**:
```json
{
 "customer": {
 "name": "O'Brien & Associates <CEO>",
 "notes": "Customer said: \"Great service!\""
 }
}
```

**Expected**: Special characters should be properly escaped in XML output:
```xml
<Customer>
 <Name>O&apos;Brien &amp; Associates &lt;CEO&gt;</Name>
 <Notes>Customer said: &quot;Great service!&quot;</Notes>
</Customer>
```

#### Very Long Values

**Input**:
```json
{
 "customer": {
 "notes": "Lorem ipsum dolor sit amet, consectetur adipiscing elit..." // 5000 characters
 }
}
```

**Expected**: Long values should be handled without truncation (unless configured).

#### Arrays

**Empty Array**:
```json
{
 "order": {
 "items": []
 }
}
```

**Single Item**:
```json
{
 "order": {
 "items": [
 {"sku": "ABC123", "qty": 1}
 ]
 }
}
```

**Multiple Items**:
```json
{
 "order": {
 "items": [
 {"sku": "ABC123", "qty": 1},
 {"sku": "XYZ789", "qty": 2},
 {"sku": "DEF456", "qty": 3}
 ]
 }
}
```

### Transformer Testing

Test each transformer individually:

#### ToUpper
- Input: "john doe" → Expected: "JOHN DOE"
- Input: "ALREADY UPPER" → Expected: "ALREADY UPPER"
- Input: "" → Expected: ""

#### FormatPhone
- Input: "5551234567" → Expected: "(555) 123-4567"
- Input: "(555) 123-4567" → Expected: "(555) 123-4567"
- Input: "555-123-4567" → Expected: "(555) 123-4567"
- Input: "invalid" → Expected: "invalid" (no change)

#### BooleanToYN
- Input: true → Expected: "Y"
- Input: false → Expected: "N"
- Input: "true" → Expected: "Y"
- Input: "1" → Expected: "Y"

### Complex Scenarios

#### Nested Objects

**Input**:
```json
{
 "order": {
 "customer": {
 "name": "John Doe",
 "address": {
 "street": "123 Main St",
 "city": "Boston",
 "state": "MA"
 }
 }
 }
}
```

**Expected**: Properly nested output structure.

#### Data Type Conversions

**Input**:
```json
{
 "product": {
 "price": 19.99,
 "quantity": 5,
 "inStock": true,
 "rating": 4.5
 }
}
```

**Expected**: Numbers and booleans converted to strings appropriately.

## Overriding Static Values

Test mode allows overriding static values to test different scenarios:

**Normal Static Values**:
```json
{
 "Customer.Source": "API",
 "Customer.Environment": "PROD"
}
```

**Override for Testing**:
```json
{
 "overrideStaticValues": {
 "Customer.Source": "TEST",
 "Customer.Environment": "DEV"
 }
}
```

**Result**:
```xml
<Customer>
 <Source>TEST</Source>
 <Environment>DEV</Environment>
</Customer>
```

## Generating Sample Data

Create realistic test data:

### Simple Generator

**Customer Example**:
```json
{
 "customer": {
 "id": "C00001",
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567",
 "dateOfBirth": "1985-06-15",
 "isActive": true,
 "accountBalance": 1250.75
 }
}
```

### Complex Generator (with arrays)

**Order Example**:
```json
{
 "order": {
 "orderId": "ORD-2024-001",
 "orderDate": "2024-01-15T10:30:00Z",
 "customer": {
 "customerId": "C00001",
 "name": "John Doe",
 "email": "john.doe@example.com"
 },
 "items": [
 {
 "lineNumber": 1,
 "sku": "WIDGET-A",
 "description": "Widget Type A",
 "quantity": 2,
 "unitPrice": 19.99,
 "total": 39.98
 },
 {
 "lineNumber": 2,
 "sku": "WIDGET-B",
 "description": "Widget Type B",
 "quantity": 1,
 "unitPrice": 29.99,
 "total": 29.99
 }
 ],
 "subtotal": 69.97,
 "tax": 4.90,
 "total": 74.87
 }
}
```

### Using Schema-Based Generators

If you have JSON schemas, use tools to generate sample data:

```bash
# Using npm package json-schema-faker
npm install -g json-schema-faker
jsf schema.json > sample.json
```

## Comparing Expected vs Actual Output

Save expected outputs and compare:

**expected-output.xml**:
```xml
<Customer>
 <FirstName>John</FirstName>
 <LastName>DOE</LastName>
 <Email>john.doe@example.com</Email>
</Customer>
```

**Comparison**:
1. Run test with sample input
2. Copy actual output
3. Compare with expected output using diff tool
4. Identify discrepancies

## Automated Testing

Integrate test mode into automated tests:

**Example Unit Test**:
```csharp
[Test]
public async Task TestCustomerIntegration()
{
 // Arrange
 var client = new IntegrationApiClient("http://localhost:5074");
 var integrationId = Guid.Parse("...");

 var testRequest = new TestMappingRequest
 {
 SamplePayload = @"{
 ""customer"": {
 ""firstName"": ""John"",
 ""lastName"": ""Doe""
 }
 }"
 };

 // Act
 var result = await client.TestIntegrationAsync(integrationId, testRequest);

 // Assert
 Assert.IsTrue(result.Success);
 Assert.IsNotNull(result.TransformedPayload);
 StringAssert.Contains("<FirstName>John</FirstName>", result.TransformedPayload);
 StringAssert.Contains("<LastName>DOE</LastName>", result.TransformedPayload);
}
```

## Best Practices

### 1. Test Early and Often

Test integrations during development, not just before deployment.

### 2. Create a Test Suite

Build a collection of test cases covering:
- Happy path scenarios
- Edge cases
- Error conditions
- Different data types
- Boundary values

### 3. Use Real Production Data (Anonymized)

Copy actual payloads from production (with sensitive data redacted) to test with realistic data.

### 4. Version Control Test Cases

Store test inputs and expected outputs in version control:

```
/tests/integration-tests/
 customer-integration/
 test-001-happy-path.json
 test-001-expected.xml
 test-002-empty-values.json
 test-002-expected.xml
 test-003-special-chars.json
 test-003-expected.xml
```

### 5. Document Test Scenarios

Document what each test case is validating:

```json
{
 "testCase": "Special Characters",
 "description": "Verifies XML escaping of special characters like <, >, &, and quotes",
 "input": {...},
 "expectedOutput": {...}
}
```

### 6. Regression Testing

When fixing bugs:
1. Add a test case that reproduces the bug
2. Fix the integration
3. Verify the test now passes
4. Keep the test to prevent regression

## Limitations

### What Test Mode Does NOT Do

1. **Does not call destination systems**
 - SOAP endpoints are not contacted
 - gRPC services are not invoked
 - Messages are not sent to queues

2. **Does not test authentication**
 - Auth behaviors run but don't make external calls
 - Cannot verify credentials are valid

3. **Does not test network issues**
 - Timeouts, connection failures, etc.
 - Cannot test retry logic

4. **Does not test destination validation**
 - Cannot verify destination accepts the payload
 - Doesn't validate against destination schemas

### When to Use End-to-End Testing

Use actual integration (not test mode) for:
- Verifying destination connectivity
- Testing authentication credentials
- Validating against destination system schemas
- Testing error handling for destination failures
- Load testing

## Troubleshooting

### Test Returns Empty Output

**Causes**:
- Source paths don't match input structure
- Transformers failing silently
- Output format misconfigured

**Solutions**:
1. Check the Debug tab for details
2. Verify source path syntax
3. Test with simpler input
4. Check transformer logs

### Transformer Not Applied

**Causes**:
- Transformer not registered
- Incorrect transformer name
- Transformer failing

**Solutions**:
1. Check transformer name spelling
2. Verify transformer is in DI container
3. Review Debug tab for transformer execution
4. Check application logs for errors

### Invalid XML Output

**Causes**:
- Special characters not escaped
- Invalid element names
- Missing namespace declarations

**Solutions**:
1. Use XML validator to identify issues
2. Verify special character handling
3. Check namespace configuration
4. Review destination path syntax

## Next Steps

- [Field Mappings](field-mappings.md) - Master field mapping syntax
- [Transformers](transformers.md) - Learn about transformers
- [Message Capture](message-capture.md) - Debug with message capture
- [Creating Integrations](creating-integrations.md) - Build integrations
