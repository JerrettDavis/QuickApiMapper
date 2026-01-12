# Custom Transformers for Demo Scenarios

This project contains custom transformers designed for demonstration purposes in the QuickApiMapper system.

## Available Transformers

### 1. PriorityMapperTransformer

**Name:** `priorityMapper`

**Purpose:** Maps e-commerce priority values to warehouse codes.

**Mappings:**
- `STANDARD` → `STD`
- `EXPRESS` → `EXP`
- `OVERNIGHT` → `OVN`
- Any other value → `STD` (default)

**Arguments:** None required

**Example Usage:**
```json
{
  "sourceField": "priority",
  "destinationField": "warehouseCode",
  "transformers": [
    {
      "name": "priorityMapper"
    }
  ]
}
```

**Example Transformations:**
- Input: `"STANDARD"` → Output: `"STD"`
- Input: `"EXPRESS"` → Output: `"EXP"`
- Input: `"express"` (lowercase) → Output: `"EXP"`
- Input: `"UNKNOWN"` → Output: `"STD"`
- Input: `null` → Output: `"STD"`

---

### 2. CurrencyFormatterTransformer

**Name:** `currencyFormatter`

**Purpose:** Formats currency values with proper decimal places and currency codes.

**Supported Currencies:** USD, EUR, GBP

**Arguments:**
- `currency` (optional): The currency code (USD, EUR, or GBP). Defaults to "USD" if not specified.

**Example Usage:**
```json
{
  "sourceField": "price",
  "destinationField": "formattedPrice",
  "transformers": [
    {
      "name": "currencyFormatter",
      "args": {
        "currency": "USD"
      }
    }
  ]
}
```

**Example Transformations:**
- Input: `"599.99"`, currency: `"USD"` → Output: `"599.99 USD"`
- Input: `"1234.5"`, currency: `"EUR"` → Output: `"1234.50 EUR"`
- Input: `"42"`, currency: `"GBP"` → Output: `"42.00 GBP"`
- Input: `"599.99"`, no args → Output: `"599.99 USD"` (default)
- Input: `"invalid"` → Output: `""`

---

### 3. OrderIdGeneratorTransformer

**Name:** `orderIdGenerator`

**Purpose:** Transforms order IDs by adding configurable prefix and/or suffix.

**Arguments:**
- `prefix` (optional): Text to add before the order ID
- `suffix` (optional): Text to add after the order ID

**Example Usage:**

**With prefix:**
```json
{
  "sourceField": "orderId",
  "destinationField": "formattedOrderId",
  "transformers": [
    {
      "name": "orderIdGenerator",
      "args": {
        "prefix": "ORD-2026-"
      }
    }
  ]
}
```

**With both prefix and suffix:**
```json
{
  "sourceField": "orderId",
  "destinationField": "formattedOrderId",
  "transformers": [
    {
      "name": "orderIdGenerator",
      "args": {
        "prefix": "ORD-",
        "suffix": "-2026"
      }
    }
  ]
}
```

**Example Transformations:**
- Input: `"12345"`, prefix: `"ORD-2026-"` → Output: `"ORD-2026-12345"`
- Input: `"12345"`, suffix: `"-WEB"` → Output: `"12345-WEB"`
- Input: `"12345"`, prefix: `"ORD-"`, suffix: `"-2026"` → Output: `"ORD-12345-2026"`
- Input: `"12345"`, no args → Output: `"12345"`
- Input: `null` → Output: `""`

---

## Complete Demo Scenario Example

Here's a complete mapping configuration that uses all three custom transformers:

```json
{
  "sourceName": "E-Commerce Order API",
  "destinationName": "Warehouse Management System",
  "fieldMappings": [
    {
      "sourceField": "orderId",
      "destinationField": "warehouseOrderId",
      "transformers": [
        {
          "name": "orderIdGenerator",
          "args": {
            "prefix": "ORD-2026-"
          }
        }
      ]
    },
    {
      "sourceField": "shippingPriority",
      "destinationField": "warehouseCode",
      "transformers": [
        {
          "name": "priorityMapper"
        }
      ]
    },
    {
      "sourceField": "totalAmount",
      "destinationField": "formattedTotal",
      "transformers": [
        {
          "name": "currencyFormatter",
          "args": {
            "currency": "USD"
          }
        }
      ]
    }
  ]
}
```

**Sample Input:**
```json
{
  "orderId": "12345",
  "shippingPriority": "EXPRESS",
  "totalAmount": "599.99"
}
```

**Sample Output:**
```json
{
  "warehouseOrderId": "ORD-2026-12345",
  "warehouseCode": "EXP",
  "formattedTotal": "599.99 USD"
}
```

---

## Development Notes

### Transformer Discovery

These transformers are automatically discovered by the QuickApiMapper system through:

1. **Reflection-based discovery**: The `AddQuickApiMapper` method scans all loaded assemblies with "QuickApiMapper" in their name
2. **Directory-based loading**: The DLL is copied to the `Transformers` folder during build
3. **Attribute marking**: The `[UsedImplicitly]` attribute ensures the transformers aren't optimized away

### Error Handling

All transformers follow these error handling principles:

- **Null/empty inputs**: Return empty string or sensible default
- **Invalid formats**: Return empty string (currency formatter) or default value (priority mapper)
- **Missing arguments**: Use sensible defaults (e.g., USD for currency, no modification for order ID)

### Testing

Run the test suite to verify transformer behavior:

```bash
dotnet test tests/QuickApiMapper.UnitTests/QuickApiMapper.UnitTests.csproj
```

### Building

To build the CustomTransformers project:

```bash
dotnet build src/QuickApiMapper.CustomTransformers/QuickApiMapper.CustomTransformers.csproj
```

The build automatically copies the DLL to `src/QuickApiMapper.Web/Transformers/` for runtime discovery.
