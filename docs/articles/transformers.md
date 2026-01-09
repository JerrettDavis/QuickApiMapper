# Transformers

Transformers modify field values during the mapping process. This guide covers built-in transformers, creating custom transformers, and advanced transformation scenarios.

## Overview

Transformers are applied to field values during mapping, allowing you to:

- Format data (phone numbers, dates, currency)
- Convert data types (string to boolean, boolean to Y/N)
- Transform text (uppercase, lowercase, trim)
- Perform calculations
- Apply business logic

## Built-in Transformers

QuickApiMapper includes standard transformers for common scenarios.

### Text Transformers

#### ToUpper
Converts text to uppercase.

**Example**:
```
Input: "john doe"
Output: "JOHN DOE"
```

**Configuration**:
```json
{
 "transformerName": "ToUpper",
 "parameters": {}
}
```

#### ToLower
Converts text to lowercase.

**Example**:
```
Input: "JOHN DOE"
Output: "john doe"
```

#### Trim
Removes leading and trailing whitespace.

**Example**:
```
Input: " John Doe "
Output: "John Doe"
```

### Boolean Transformers

#### ToBool / ToBoolean
Converts string values to boolean.

**Truthy Values**: "true", "1", "yes", "y", "on"
**Falsy Values**: "false", "0", "no", "n", "off", empty string

**Example**:
```
Input: "yes"
Output: true

Input: "0"
Output: false
```

**Configuration**:
```json
{
 "transformerName": "ToBoolean",
 "parameters": {}
}
```

#### BooleanToYN
Converts boolean to "Y" or "N" string.

**Example**:
```
Input: true
Output: "Y"

Input: false
Output: "N"
```

**Configuration**:
```json
{
 "transformerName": "BooleanToYN",
 "parameters": {}
}
```

**Location**: `QuickApiMapper.CustomTransformers/BooleanToYNTransformer.cs`

### Formatting Transformers

#### FormatPhone
Formats phone numbers to standard format.

**Supported Input Formats**:
- `5551234567`
- `555-123-4567`
- `(555) 123-4567`
- `+1 555 123 4567`

**Output Format**: `(555) 123-4567`

**Example**:
```
Input: "5551234567"
Output: "(555) 123-4567"

Input: "555-123-4567"
Output: "(555) 123-4567"
```

**Configuration**:
```json
{
 "transformerName": "FormatPhone",
 "parameters": {}
}
```

**Location**: `QuickApiMapper.StandardTransformers/FormatPhoneTransformer.cs`

## Chaining Transformers

You can chain multiple transformers to execute in sequence.

**Example**:
```json
{
 "sourcePath": "$.customer.name",
 "destinationPath": "Customer.Name",
 "transformers": [
 {"transformerName": "Trim"},
 {"transformerName": "ToUpper"}
 ]
}
```

**Execution**:
```
Input: " john doe "
After Trim: "john doe"
After ToUpper: "JOHN DOE"
Output: "JOHN DOE"
```

## Creating Custom Transformers

You can create custom transformers to implement business-specific logic.

### Step 1: Implement the Transformer Class

Create a class that inherits from `Transformer`:

```csharp
using QuickApiMapper.Contracts;

namespace MyCompany.CustomTransformers;

public class PrefixTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 if (string.IsNullOrEmpty(input))
 return input;

 // Get parameter from transformer configuration
 var prefix = Parameters.GetValueOrDefault("prefix", "PREFIX-");

 return $"{prefix}{input}";
 }
}
```

### Step 2: Register the Transformer

Register your transformer in the DI container:

**QuickApiMapper.Web/Program.cs**:
```csharp
using MyCompany.CustomTransformers;

builder.Services.AddTransformer<PrefixTransformer>();
```

### Step 3: Use in Integration

Reference your transformer in field mappings:

```json
{
 "sourcePath": "$.order.id",
 "destinationPath": "Order.OrderNumber",
 "transformers": [
 {
 "transformerName": "Prefix",
 "parameters": {
 "prefix": "ORD-"
 }
 }
 ]
}
```

**Result**:
```
Input: "12345"
Output: "ORD-12345"
```

## Advanced Custom Transformers

### Accessing Context

The `MappingContext` provides access to the entire source payload and configuration.

```csharp
public class CalculateTotalTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 // Access other fields from source
 var quantity = int.Parse(context.ResolveSourcePath("$.item.quantity"));
 var price = decimal.Parse(context.ResolveSourcePath("$.item.price"));

 var total = quantity * price;
 return total.ToString("F2");
 }
}
```

### Parameterized Transformers

Accept parameters from configuration:

```csharp
public class RoundTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 if (!decimal.TryParse(input, out var value))
 return input;

 // Get decimal places from parameters (default to 2)
 var decimals = int.Parse(Parameters.GetValueOrDefault("decimals", "2"));

 return Math.Round(value, decimals).ToString($"F{decimals}");
 }
}
```

**Usage**:
```json
{
 "transformerName": "Round",
 "parameters": {
 "decimals": "3"
 }
}
```

### Conditional Transformers

Implement conditional logic:

```csharp
public class ConditionalMapTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 // Map input values to output values
 var mapping = new Dictionary<string, string>
 {
 ["individual"] = "I",
 ["business"] = "B",
 ["government"] = "G"
 };

 return mapping.TryGetValue(input?.ToLower() ?? "", out var mapped)
 ? mapped
 : input; // Return original if no mapping found
 }
}
```

### Async Transformers

Perform async operations (database lookups, API calls):

```csharp
public class EnrichCustomerTransformer : Transformer
{
 private readonly ICustomerRepository _customerRepo;

 public EnrichCustomerTransformer(ICustomerRepository customerRepo)
 {
 _customerRepo = customerRepo;
 }

 public override async Task<string> TransformAsync(
 string input,
 MappingContext context)
 {
 // Look up customer by ID
 var customer = await _customerRepo.GetByIdAsync(input);

 return customer?.FullName ?? input;
 }
}
```

## Real-World Examples

### Example 1: SSN Masking

```csharp
public class MaskSsnTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 if (string.IsNullOrEmpty(input) || input.Length < 4)
 return input;

 // Mask all but last 4 digits
 var lastFour = input.Substring(input.Length - 4);
 return $"XXX-XX-{lastFour}";
 }
}
```

**Usage**:
```
Input: "123456789"
Output: "XXX-XX-6789"
```

### Example 2: Date Formatting

```csharp
public class FormatDateTransformer : Transformer
{
 public override string Transform(string input, MappingContext context)
 {
 if (!DateTime.TryParse(input, out var date))
 return input;

 // Get format from parameters (default ISO 8601)
 var format = Parameters.GetValueOrDefault("format", "yyyy-MM-dd");

 return date.ToString(format);
 }
}
```

**Usage**:
```json
{
 "transformerName": "FormatDate",
 "parameters": {
 "format": "MM/dd/yyyy"
 }
}
```

```
Input: "2026-01-08T10:00:00Z"
Output: "01/08/2026"
```

### Example 3: Currency Conversion

```csharp
public class ConvertCurrencyTransformer : Transformer
{
 private readonly ICurrencyService _currencyService;

 public ConvertCurrencyTransformer(ICurrencyService currencyService)
 {
 _currencyService = currencyService;
 }

 public override async Task<string> TransformAsync(
 string input,
 MappingContext context)
 {
 if (!decimal.TryParse(input, out var amount))
 return input;

 var fromCurrency = Parameters.GetValueOrDefault("from", "USD");
 var toCurrency = Parameters.GetValueOrDefault("to", "EUR");

 var rate = await _currencyService.GetExchangeRateAsync(
 fromCurrency,
 toCurrency);

 var converted = amount * rate;
 return converted.ToString("F2");
 }
}
```

**Usage**:
```json
{
 "transformerName": "ConvertCurrency",
 "parameters": {
 "from": "USD",
 "to": "EUR"
 }
}
```

### Example 4: Lookup Transformer

```csharp
public class LookupTransformer : Transformer
{
 private readonly ILookupService _lookupService;

 public LookupTransformer(ILookupService lookupService)
 {
 _lookupService = lookupService;
 }

 public override async Task<string> TransformAsync(
 string input,
 MappingContext context)
 {
 var lookupType = Parameters.GetValueOrDefault("type", "");

 var value = await _lookupService.GetValueAsync(lookupType, input);
 return value ?? input;
 }
}
```

**Usage**:
```json
{
 "transformerName": "Lookup",
 "parameters": {
 "type": "CountryCode"
 }
}
```

```
Input: "US"
Output: "United States"
```

## Transformer Best Practices

### 1. Keep Transformers Focused

Each transformer should do one thing well:

 **Good**:
- `ToUpper` - Only converts to uppercase
- `Trim` - Only removes whitespace

 **Bad**:
- `FormatAndValidateAndConvert` - Does too many things

### 2. Handle Null and Empty Values

Always check for null or empty input:

```csharp
public override string Transform(string input, MappingContext context)
{
 if (string.IsNullOrEmpty(input))
 return input; // or return default value

 // Transformation logic
}
```

### 3. Use Parameters for Configuration

Make transformers reusable with parameters:

```csharp
var separator = Parameters.GetValueOrDefault("separator", ",");
```

### 4. Add Logging

Log errors and warnings:

```csharp
public class MyTransformer : Transformer
{
 private readonly ILogger<MyTransformer> _logger;

 public MyTransformer(ILogger<MyTransformer> logger)
 {
 _logger = logger;
 }

 public override string Transform(string input, MappingContext context)
 {
 try
 {
 // Transformation logic
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Transformation failed for input: {Input}", input);
 return input; // Return original value on error
 }
 }
}
```

### 5. Make Transformers Testable

Write unit tests for transformers:

```csharp
[Test]
public void ToUpper_ConvertsToUppercase()
{
 // Arrange
 var transformer = new ToUpperTransformer();
 var context = new MappingContext();

 // Act
 var result = transformer.Transform("hello", context);

 // Assert
 Assert.AreEqual("HELLO", result);
}
```

### 6. Document Parameters

Document expected parameters in code comments:

```csharp
/// <summary>
/// Formats a date string.
/// </summary>
/// <param name="input">Date string to format</param>
/// <param name="context">Mapping context</param>
/// <returns>Formatted date string</returns>
/// <remarks>
/// Parameters:
/// - format: Output date format (default: "yyyy-MM-dd")
/// </remarks>
public class FormatDateTransformer : Transformer
{
 // Implementation
}
```

## Transformer Registry

Transformers are registered in the `TransformerRegistry` and cached for performance.

### Registering Multiple Transformers

```csharp
// Register all transformers from an assembly
builder.Services.AddTransformersFromAssembly(typeof(MyTransformer).Assembly);

// Or register individually
builder.Services.AddTransformer<Transformer1>();
builder.Services.AddTransformer<Transformer2>();
builder.Services.AddTransformer<Transformer3>();
```

### Dynamic Transformer Loading

Load transformers from external assemblies:

```csharp
var assembly = Assembly.LoadFrom("path/to/CustomTransformers.dll");
builder.Services.AddTransformersFromAssembly(assembly);
```

## Troubleshooting

### Transformer Not Found

**Error**: `Transformer 'MyTransformer' not found`

**Solutions**:
- Ensure transformer is registered in DI container
- Check transformer name matches class name (without "Transformer" suffix)
- Verify transformer assembly is referenced

### Transformation Failed

**Error**: `Transformer 'ToUpper' failed to transform value`

**Solutions**:
- Check input value is not null
- Verify input type matches transformer expectations
- Add try-catch in transformer to handle errors gracefully

### Parameter Not Applied

**Issue**: Parameters are ignored

**Solutions**:
- Ensure `Parameters` property is accessed in transformer
- Verify parameter name spelling
- Check parameter values are strings in JSON

## Next Steps

- [Creating Integrations](creating-integrations.md) - Use transformers in integrations
- [Behaviors](behaviors.md) - Add cross-cutting concerns with behaviors
- [Architecture](architecture.md) - Understand how transformers fit in the pipeline
