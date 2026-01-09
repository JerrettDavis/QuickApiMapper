# Field Mappings

Field mappings define how data is transformed from the source format to the destination format. This guide covers mapping syntax, advanced scenarios, and best practices.

## Overview

A field mapping consists of three main components:

1. **Source Path** - Location in the source data (JSONPath or XPath)
2. **Destination Path** - Location in the destination data (dot notation)
3. **Transformers** - Optional data transformations applied to the value

## Source Path Syntax

### JSON Source (JSONPath)

QuickApiMapper uses JSONPath syntax for navigating JSON documents.

#### Basic Property Access

```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe"
 }
}
```

**Mappings**:
- `$.customer.firstName` → "John"
- `$.customer.lastName` → "Doe"

#### Nested Objects

```json
{
 "order": {
 "customer": {
 "address": {
 "street": "123 Main St",
 "city": "Boston"
 }
 }
 }
}
```

**Mappings**:
- `$.order.customer.address.street` → "123 Main St"
- `$.order.customer.address.city` → "Boston"

#### Array Access

**By Index**:
```json
{
 "items": [
 {"sku": "ABC123", "quantity": 2},
 {"sku": "XYZ789", "quantity": 5}
 ]
}
```

**Mappings**:
- `$.items[0].sku` → "ABC123"
- `$.items[1].quantity` → 5

**All Elements**:
- `$.items[*].sku` → Returns array of all SKUs
- `$.items[*].quantity` → Returns array of all quantities

#### Recursive Descent

Find all matching properties at any level:

```json
{
 "customer": {
 "email": "customer@example.com",
 "contact": {
 "email": "contact@example.com"
 }
 }
}
```

**Mapping**:
- `$..email` → Returns all email addresses

#### Filter Expressions

Select items based on conditions:

```json
{
 "items": [
 {"name": "Item 1", "price": 10, "inStock": true},
 {"name": "Item 2", "price": 20, "inStock": false},
 {"name": "Item 3", "price": 15, "inStock": true}
 ]
}
```

**Mappings**:
- `$.items[?(@.inStock)].name` → Items in stock
- `$.items[?(@.price > 15)].name` → Items over $15

### XML Source (XPath)

QuickApiMapper uses XPath syntax for navigating XML documents.

#### Basic Element Access

```xml
<Customer>
 <FirstName>John</FirstName>
 <LastName>Doe</LastName>
</Customer>
```

**Mappings**:
- `/Customer/FirstName` → "John"
- `/Customer/LastName` → "Doe"

#### Nested Elements

```xml
<Order>
 <Customer>
 <Address>
 <Street>123 Main St</Street>
 <City>Boston</City>
 </Address>
 </Customer>
</Order>
```

**Mappings**:
- `/Order/Customer/Address/Street` → "123 Main St"
- `/Order/Customer/Address/City` → "Boston"

#### Attributes

```xml
<Customer id="12345" type="individual">
 <Name>John Doe</Name>
</Customer>
```

**Mappings**:
- `/Customer/@id` → "12345"
- `/Customer/@type` → "individual"
- `/Customer/Name` → "John Doe"

#### Array Elements

```xml
<Order>
 <Items>
 <Item>
 <SKU>ABC123</SKU>
 <Quantity>2</Quantity>
 </Item>
 <Item>
 <SKU>XYZ789</SKU>
 <Quantity>5</Quantity>
 </Item>
 </Items>
</Order>
```

**Mappings**:
- `/Order/Items/Item[1]/SKU` → "ABC123" (1-based index)
- `/Order/Items/Item[2]/Quantity` → "5"
- `//Item/SKU` → All SKUs

## Destination Path Syntax

Destination paths use dot notation to define the output structure.

### Simple Properties

**Mapping**: `Customer.FirstName`

**Output (JSON)**:
```json
{
 "Customer": {
 "FirstName": "John"
 }
}
```

**Output (XML)**:
```xml
<Customer>
 <FirstName>John</FirstName>
</Customer>
```

### Nested Objects

**Mapping**: `Customer.ContactInfo.Email`

**Output (JSON)**:
```json
{
 "Customer": {
 "ContactInfo": {
 "Email": "john@example.com"
 }
 }
}
```

**Output (XML)**:
```xml
<Customer>
 <ContactInfo>
 <Email>john@example.com</Email>
 </ContactInfo>
</Customer>
```

### Arrays

**Mappings**:
- `Order.Items.Item[0].SKU` → First item
- `Order.Items.Item[1].SKU` → Second item

**Output (XML)**:
```xml
<Order>
 <Items>
 <Item>
 <SKU>ABC123</SKU>
 </Item>
 <Item>
 <SKU>XYZ789</SKU>
 </Item>
 </Items>
</Order>
```

### XML Attributes

Use `@` prefix for attributes (XML destination only):

**Mapping**: `Customer.@id`

**Output**:
```xml
<Customer id="12345">
</Customer>
```

## Complete Mapping Examples

### Example 1: Customer JSON to SOAP XML

**Source (JSON)**:
```json
{
 "customer": {
 "id": "C12345",
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

**Field Mappings**:

| Source Path | Destination Path | Transformers |
|-------------|------------------|--------------|
| `$.customer.id` | `Customer.@customerId` | (none) |
| `$.customer.firstName` | `Customer.Name.FirstName` | (none) |
| `$.customer.lastName` | `Customer.Name.LastName` | ToUpper |
| `$.customer.email` | `Customer.Contact.Email` | ToLower |
| `$.customer.phone` | `Customer.Contact.Phone` | FormatPhone |
| `$.customer.address.street` | `Customer.Address.Street` | (none) |
| `$.customer.address.city` | `Customer.Address.City` | (none) |
| `$.customer.address.state` | `Customer.Address.State` | (none) |
| `$.customer.address.zip` | `Customer.Address.ZipCode` | (none) |
| `$.customer.isActive` | `Customer.Status` | BooleanToYN |

**Static Values**:
- `Customer.Source` = "API"
- `Customer.Type` = "Individual"

**Output (XML)**:
```xml
<Customer customerId="C12345">
 <Name>
 <FirstName>John</FirstName>
 <LastName>DOE</LastName>
 </Name>
 <Contact>
 <Email>john.doe@example.com</Email>
 <Phone>(555) 123-4567</Phone>
 </Contact>
 <Address>
 <Street>123 Main St</Street>
 <City>Boston</City>
 <State>MA</State>
 <ZipCode>02101</ZipCode>
 </Address>
 <Status>Y</Status>
 <Source>API</Source>
 <Type>Individual</Type>
</Customer>
```

### Example 2: Order with Line Items

**Source (JSON)**:
```json
{
 "order": {
 "orderId": "ORD-2024-001",
 "orderDate": "2024-01-15T10:30:00Z",
 "customer": {
 "customerId": "C12345",
 "name": "John Doe"
 },
 "items": [
 {
 "sku": "WIDGET-A",
 "description": "Widget Type A",
 "quantity": 2,
 "unitPrice": 19.99
 },
 {
 "sku": "WIDGET-B",
 "description": "Widget Type B",
 "quantity": 1,
 "unitPrice": 29.99
 }
 ],
 "total": 69.97
 }
}
```

**Field Mappings**:

| Source Path | Destination Path | Transformers |
|-------------|------------------|--------------|
| `$.order.orderId` | `Order.OrderNumber` | (none) |
| `$.order.orderDate` | `Order.OrderDate` | FormatDate |
| `$.order.customer.customerId` | `Order.Customer.CustomerId` | (none) |
| `$.order.customer.name` | `Order.Customer.Name` | (none) |
| `$.order.items[0].sku` | `Order.LineItems.LineItem[0].SKU` | (none) |
| `$.order.items[0].description` | `Order.LineItems.LineItem[0].Description` | (none) |
| `$.order.items[0].quantity` | `Order.LineItems.LineItem[0].Quantity` | (none) |
| `$.order.items[0].unitPrice` | `Order.LineItems.LineItem[0].UnitPrice` | (none) |
| `$.order.items[1].sku` | `Order.LineItems.LineItem[1].SKU` | (none) |
| `$.order.items[1].description` | `Order.LineItems.LineItem[1].Description` | (none) |
| `$.order.items[1].quantity` | `Order.LineItems.LineItem[1].Quantity` | (none) |
| `$.order.items[1].unitPrice` | `Order.LineItems.LineItem[1].UnitPrice` | (none) |
| `$.order.total` | `Order.Total` | (none) |

**Output (XML)**:
```xml
<Order>
 <OrderNumber>ORD-2024-001</OrderNumber>
 <OrderDate>2024-01-15</OrderDate>
 <Customer>
 <CustomerId>C12345</CustomerId>
 <Name>John Doe</Name>
 </Customer>
 <LineItems>
 <LineItem>
 <SKU>WIDGET-A</SKU>
 <Description>Widget Type A</Description>
 <Quantity>2</Quantity>
 <UnitPrice>19.99</UnitPrice>
 </LineItem>
 <LineItem>
 <SKU>WIDGET-B</SKU>
 <Description>Widget Type B</Description>
 <Quantity>1</Quantity>
 <UnitPrice>29.99</UnitPrice>
 </LineItem>
 </LineItems>
 <Total>69.97</Total>
</Order>
```

## Advanced Mapping Scenarios

### Flattening Nested Structures

**Source**:
```json
{
 "customer": {
 "name": {
 "first": "John",
 "middle": "Q",
 "last": "Doe"
 }
 }
}
```

**Mappings**:
- `$.customer.name.first` → `Customer.FirstName`
- `$.customer.name.middle` → `Customer.MiddleInitial`
- `$.customer.name.last` → `Customer.LastName`

**Output**:
```xml
<Customer>
 <FirstName>John</FirstName>
 <MiddleInitial>Q</MiddleInitial>
 <LastName>Doe</LastName>
</Customer>
```

### Creating Nested Structures from Flat Data

**Source**:
```json
{
 "firstName": "John",
 "lastName": "Doe",
 "email": "john@example.com",
 "phone": "5551234567"
}
```

**Mappings**:
- `$.firstName` → `Customer.Name.FirstName`
- `$.lastName` → `Customer.Name.LastName`
- `$.email` → `Customer.Contact.Email`
- `$.phone` → `Customer.Contact.Phone`

**Output**:
```xml
<Customer>
 <Name>
 <FirstName>John</FirstName>
 <LastName>Doe</LastName>
 </Name>
 <Contact>
 <Email>john@example.com</Email>
 <Phone>5551234567</Phone>
 </Contact>
</Customer>
```

### Renaming Fields

Simply map source to different destination:

- `$.cust_id` → `Customer.CustomerId`
- `$.f_name` → `Customer.FirstName`
- `$.l_name` → `Customer.LastName`

### Type Conversions

Use transformers for type conversions:

- `$.isActive` → `Customer.Active` (Transformer: ToBoolean)
- `$.status` → `Customer.IsEnabled` (Transformer: BooleanToYN)
- `$.price` → `Product.Price` (Transformer: Round)

### Conditional Mappings

Use custom transformers for conditional logic:

```csharp
public class CustomerTypeTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 var customerType = context.ResolveSourcePath("$.customer.type");
 var taxId = context.ResolveSourcePath("$.customer.taxId");

 if (!string.IsNullOrEmpty(taxId))
 return "Business";

 return "Individual";
 }
}
```

## Best Practices

### 1. Use Clear, Descriptive Paths

 **Good**:
- `$.customer.billingAddress.street`
- `Customer.BillingAddress.StreetLine1`

 **Bad**:
- `$.c.ba.s`
- `Customer.addr1`

### 2. Keep Mappings Simple

Each mapping should do one thing. Use multiple mappings instead of complex logic in one transformer.

 **Good**:
```
$.firstName → Customer.FirstName (ToUpper)
$.lastName → Customer.LastName (ToUpper)
```

 **Bad**:
```
$.fullName → Customer.FullName (CustomSplitAndFormat transformer)
```

### 3. Handle Null Values

Ensure transformers handle null input gracefully:

```csharp
public override string Transform(string input, MappingContext context)
{
 if (string.IsNullOrEmpty(input))
 return string.Empty; // or return input;

 // Transformation logic
}
```

### 4. Test with Real Data

Always test mappings with actual data from your source system, including:
- Empty values
- Null values
- Special characters
- Edge cases (very long strings, negative numbers, etc.)

### 5. Document Complex Mappings

Add integration descriptions explaining non-obvious mappings:

```json
{
 "description": "Customer integration - Maps Salesforce customer format to ERP. Note: CustomerType is derived from presence of TaxId field."
}
```

### 6. Use Static Values Appropriately

Static values are good for:
- Source system identifiers
- Environment markers
- Fixed codes required by destination

Avoid using static values for:
- Data that might change
- Values that should come from source

### 7. Optimize for Performance

- Avoid recursive descent (`$..`) when possible - use specific paths
- Minimize the number of transformers per field
- Cache integration configs in production

## Troubleshooting

### Source Value Not Resolved

**Problem**: `Failed to resolve source path '$.customer.name'`

**Solutions**:
1. Verify the path syntax (JSONPath vs XPath)
2. Check the actual source data structure
3. Test the path expression with sample data
4. Ensure field names match exactly (case-sensitive)

### Incorrect Output Structure

**Problem**: Output structure doesn't match expected format

**Solutions**:
1. Review destination path syntax
2. Check for typos in property names
3. Verify array indexing is correct
4. Test with the integration test mode

### Transformer Not Applied

**Problem**: Value appears unchanged in output

**Solutions**:
1. Verify transformer is registered
2. Check transformer name spelling
3. Ensure transformer parameters are correct
4. Add logging in transformer to debug

### Array Mapping Issues

**Problem**: Only one array item appears in output

**Solutions**:
1. Create separate mappings for each array index
2. Verify array indexing syntax (`[0]`, `[1]`, etc.)
3. Check source data actually contains multiple items

## Testing Field Mappings

### Use Test Mode

Always test mappings before deploying:

1. Navigate to the integration in Web Designer
2. Click "Test Integration"
3. Paste sample source data
4. Review transformed output
5. Check the Debug tab for field-by-field transformations

### Sample Test Cases

Create test cases covering:

**Happy Path**:
- All fields populated
- Valid data types
- Normal ranges

**Edge Cases**:
- Empty strings
- Null values
- Very long strings
- Special characters (quotes, ampersands, etc.)
- Large numbers
- Arrays with 0, 1, or many items

**Error Cases**:
- Missing required fields
- Invalid data types
- Out-of-range values

## Next Steps

- [Transformers](transformers.md) - Learn about data transformations
- [Creating Integrations](creating-integrations.md) - Put mappings into practice
- [Test Mode](test-mode.md) - Test your mappings before deploying
