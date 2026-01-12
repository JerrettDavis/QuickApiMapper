# QuickApiMapper

QuickApiMapper is a lightweight, configurable gateway that transforms inbound payloads (JSON or XML) into the shape required by downstream systems (JSON, XML, or SOAP) and forwards them, using a behavior pipeline (validation, auth, HTTP config, timing) for cross-cutting concerns.

## Overview
- .NET 10 (C# latest), cross-platform ASP.NET Core Minimal API
- Declarative mappings in appsettings to transform JSON/XML into XML/JSON/SOAP
- Extensible transformers (drop-in DLLs) for custom value conversions
- Pluggable behavior pipeline: validation, OAuth/token authentication, HTTP client configuration, timing
- OpenAPI document synthesized from mappings with Scalar UI for exploration

## Prerequisites
- .NET SDK 10.0+
- Optional: PowerShell or Bash for commands
- Git (for version control and automated releases)

## Repository layout
- QuickApiMapper.Web/ — Minimal API host and OpenAPI generator
- QuickApiMapper.Application/ — Core mapping engine, destination handlers, DI wiring
- QuickApiMapper.Contracts/ — Public contracts (mappings, behaviors, engine interfaces)
- QuickApiMapper.Behaviors/ — Built‑in behaviors (Validation, Authentication, HttpClientConfiguration, Timing)
- QuickApiMapper.StandardTransformers/ — Example built‑in transformers
- QuickApiMapper.CustomTransformers/ — Example custom transformers
- QuickApiMapper.UnitTests/ — Comprehensive unit/integration tests, sample test data
- Transformers/ — Optional folder for external transformer assemblies (DLLs) loaded at runtime

## Build and run locally
1) Restore and build

```bash
# from repo root
dotnet build
```

2) Run the web host

```bash
# from repo root or the Web project folder
dotnet run --project QuickApiMapper.Web
```

3) Default endpoints
- OpenAPI JSON: GET /openapi/v1.json
- Scalar UI: app maps Scalar via `app.MapScalarApiReference()`; by default it exposes a UI route (e.g., /scalar). If not visible, use the OpenAPI JSON directly.
- Integration endpoints: one POST per configured mapping (see "Configuration"), e.g. /CustomerIntegration, /VendorIntegration in Development settings.

You can also set ASP.NET Core URLs via environment:

```bash
# Example multi‑binding
set ASPNETCORE_URLS=http://localhost:5072;https://localhost:7072
```

---

## Demo Mode

QuickApiMapper includes a comprehensive demo showcasing real-world JSON-to-SOAP transformation with a realistic e-commerce order fulfillment scenario.

### Quick Start Demo

```bash
# Start all services with Aspire
cd src/QuickApiMapper.Host.AppHost
dotnet run

# The demo includes:
# - Pre-configured integrations (JSON→SOAP, SOAP→JSON)
# - Sample e-commerce and warehouse APIs
# - Visual Designer Dashboard for monitoring
# - 10 pre-seeded sample orders
```

**Open in Browser**:
- **Designer Dashboard**: https://localhost:7002
- **Aspire Dashboard**: http://localhost:15000
- **Demo APIs**: See [DEMO_QUICK_START.md](DEMO_QUICK_START.md)

### Demo Features

The demo showcases:
- **JSON to SOAP transformation** with 16 field mappings
- **Three transformers**: Email normalization (ToLower), SKU standardization (ToUpper), Priority code mapping (MapValue)
- **Array handling**: Multi-item orders with `items[*]` notation
- **Message capture**: Complete audit trail of input/output payloads
- **RabbitMQ integration**: Async message queue processing
- **Real-time monitoring**: Live dashboard with statistics and message history
- **Production-ready performance**: Sub-200ms transformation latency

### Demo Documentation

| Document | Description |
|----------|-------------|
| [DEMO_QUICK_START.md](DEMO_QUICK_START.md) | 5-minute quick start guide |
| [docs/DEMO_GUIDE.md](docs/DEMO_GUIDE.md) | Complete demo walkthrough with step-by-step instructions |
| [docs/API_SAMPLES.md](docs/API_SAMPLES.md) | cURL commands, Postman collection, integration tests |
| [docs/ARCHITECTURE_DEMO.md](docs/ARCHITECTURE_DEMO.md) | Visual architecture with Mermaid diagrams |
| [docs/DEMO_PRESENTATION_SCRIPT.md](docs/DEMO_PRESENTATION_SCRIPT.md) | Presentation script for live demos and videos |
| [docs/DEMO_QUICK_REFERENCE.md](docs/DEMO_QUICK_REFERENCE.md) | One-page cheat sheet with all URLs and commands |
| [docs/DEMO_FAQ.md](docs/DEMO_FAQ.md) | Frequently asked questions |
| [docs/DEMO_DATA.md](docs/DEMO_DATA.md) | Complete demo data documentation |

### Demo Scenario

**Modern E-Commerce Platform** (JSON REST API) ➔ **QuickApiMapper** ➔ **Legacy Warehouse System** (SOAP Service)

**Sample Order** (JSON):
```json
{
  "orderId": "ORD-2026-001",
  "customerEmail": "JOHN.SMITH@EXAMPLE.COM",
  "items": [{"sku": "laptop-xps15", "quantity": 1, "unitPrice": 599.99}],
  "priority": "EXPRESS"
}
```

**Transformed Output** (SOAP):
```xml
<soap:Envelope>
  <soap:Body>
    <SubmitFulfillmentRequest>
      <OrderNumber>ORD-2026-001</OrderNumber>
      <CustomerInfo>
        <ContactEmail>john.smith@example.com</ContactEmail>
      </CustomerInfo>
      <LineItems>
        <Item><SKU>LAPTOP-XPS15</SKU><Qty>1</Qty></Item>
      </LineItems>
      <PriorityCode>EXP</PriorityCode>
    </SubmitFulfillmentRequest>
  </soap:Body>
</soap:Envelope>
```

**Transformations Applied**:
- Email: `JOHN.SMITH@EXAMPLE.COM` → `john.smith@example.com` (ToLower)
- SKU: `laptop-xps15` → `LAPTOP-XPS15` (ToUpper)
- Priority: `EXPRESS` → `EXP` (MapValue)

### Try the Demo

```bash
# Submit a sample order
curl -X POST http://localhost:5000/api/demo/fulfillment/submit \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "ORD-TEST-001",
    "customerName": "Jane Anderson",
    "customerEmail": "JANE@EXAMPLE.COM",
    "orderDate": "2026-01-11T10:00:00Z",
    "totalAmount": 1299.99,
    "currency": "USD",
    "items": [{"sku": "laptop-macbook-pro", "productName": "MacBook Pro", "quantity": 1, "unitPrice": 1299.99}],
    "shippingAddress": {"street": "123 Main St", "city": "Seattle", "state": "WA", "postalCode": "98101", "country": "USA"},
    "priority": "EXPRESS"
  }'

# View the transformation in the Designer Dashboard
# Open https://localhost:7002 → Message History
```

### Disable Demo Mode for Production

Demo mode is automatically disabled in non-Development environments. To explicitly disable:

```json
{
  "DemoMode": {
    "EnableDemoMode": false
  }
}
```

---

## Configuration
Configuration is sourced from appsettings.json and the environment (Development overrides in appsettings.Development.json). Key sections:

- Transformers.Directory: path to a folder where transformer DLLs are discovered (default: ./Transformers relative to the web app).
- ApiMapping: global mapping configuration
 - Namespaces: XML namespace prefixes used when building SOAP/XML
 - StaticValues: global key/value pairs referenced in mappings with `$$.Key`
 - Mappings: array of IntegrationMapping entries defining each endpoint

Example (excerpt from Development):

```json
{
 "Transformers": { "Directory": "./Transformers" },
 "ApiMapping": {
 "Namespaces": {
 "soap": "http://schemas.xmlsoap.org/soap/envelope/",
 "tns": "urn:example.com:services:vendor/v1.0"
 },
 "StaticValues": {
 "Username": "...",
 "Password": "...",
 "Profile": "DefaultProfile",
 "Data": "data"
 },
 "Mappings": [
 {
 "Name": "VendorIntegration",
 "Endpoint": "/VendorIntegration",
 "SourceType": "JSON",
 "DestinationType": "SOAP",
 "DestinationUrl": "https://example.com/api/vendor-service",
 "DispatchFor": "$.supplierinfo",
 "StaticValues": { "Vendor": "vendor", "Y": "Y" },
 "Mapping": [
 { "Source": "$.supplierinfo[0].supplier_id", "Destination": "/root/session/state/action/main/row/company_id" }
 ],
 "SoapConfig": {
 "HeaderFields": [
 { "XPath": "WrapperHeader", "Source": "", "Namespace": "urn:example.com:services:vendor/v1.0" },
 { "XPath": "WrapperHeader/User", "Source": "$$.Username", "Namespace": "urn:example.com:services:vendor/v1.0" },
 { "XPath": "WrapperHeader/Password", "Source": "$$.Password", "Namespace": "urn:example.com:services:vendor/v1.0" }
 ],
 "BodyFields": [
 { "XPath": "SendSynchronic2", "Source": "", "Namespace": "urn:example.com:services:vendor/v1.0" }
 ],
 "BodyWrapperFieldXPath": "SendSynchronic2"
 }
 }
 ]
 }
}
```

Notes
- Each mapping creates a POST endpoint at `Endpoint`.
- SourceType: JSON | XML | SOAP. DestinationType: JSON | XML | SOAP.
- DestinationUrl: where the transformed payload is forwarded.
- Static values: referenced with `$$.Key` in mapping sources and SOAP config.
- Mapping.Source uses JSONPath‑like expressions (e.g., `$.customerinfo[0].customer_id`) and static refs (`$$.Y`).
- Mapping.Destination for XML/SOAP uses an XPath‑like path rooted at the output XML’s root (e.g., `/root/session/state/...`).

### SOAP envelope configuration
- If SoapConfig is provided, the SOAP handler builds envelope/header/body from its HeaderFields and BodyFields.
- BodyWrapperFieldXPath indicates which configured body element should contain the mapped XML payload; the payload’s root is recreated in the wrapper’s namespace to avoid blank namespaces.
- If SoapConfig is omitted, a fallback envelope is generated: `<soap:Envelope><soap:Body>...mapped XML...</soap:Body></soap:Envelope>` using `StaticValues.SoapNamespace` or the default SOAP 1.1 namespace.

### OpenAPI and UI
- GET /openapi/v1.json returns a synthesized OpenAPI 3.0 document based on Mapping.Source paths (static `$$.` sources are ignored). This is for request shape guidance only.
- Scalar UI is mapped via `app.MapScalarApiReference()`; use your host’s base URL to access it. If the UI route is not obvious, fetch the JSON spec directly.

---

## Runtime behavior and request flow
1) Minimal API wires one POST route per IntegrationMapping.
2) Handler reads the request body and selects an IMappingEngine implementation based on SourceType/DestinationType.
3) The engine applies `Mapping` rules and writes to output (JObject or XDocument), reading input (JObject/XDocument) and statics.
4) A destination handler sends the result:
 - JSON destination: JsonDestinationHandler posts JSON.
 - XML/SOAP destination: SoapDestinationHandler wraps XML in a SOAP envelope (configured or fallback) and posts it as text/xml.
5) Cross‑cutting concerns use the behavior pipeline (see below) when you opt to build/execute it.

## Behaviors (cross‑cutting pipeline)
Built‑in behaviors (QuickApiMapper.Behaviors):
- ValidationBehavior (Order 50): ensures mappings and data sources exist; fails early on common config errors.
- AuthenticationBehavior (Order 100): acquires/caches tokens; sets Authorization on HttpClient.
- HttpClientConfigurationBehavior (Order 200): applies default headers, timeout, User‑Agent.
- TimingBehavior (whole‑run, Order 10): measures and records execution time in result.Properties["ExecutionTime"].

Using behaviors
- The application services include a `BehaviorPipeline` you can compose at runtime. See unit tests for examples of pre‑run, post‑run, and whole‑run composition (BehaviorIntegrationTests, BehaviorPipelineTests).
- You can also dynamically load external behavior assemblies via `AddQuickApiMapperBehaviors(behaviorDirectory)` or `AddBehaviorsFromDirectory("path")` if you opt into it in DI.

---

## Transformers
Transformers implement simple value transforms during mapping. They are discovered from:
- Project assemblies (Standard/Custom transformers), and
- External DLLs in `Transformers.Directory` (default: QuickApiMapper.Web/Transformers).

Contract
```csharp
public interface ITransformer
{
 string Name { get; }
 string Transform(string? input, IReadOnlyDictionary<string,string?>? args);
}
```
Config usage (FieldMapping.Transformers)
```json
{
 "Source": "$.supplierinfo[0].is_primary",
 "Destination": "/root/.../is_primary",
 "Transformers": [ { "Name": "booleanToYN" } ]
}
```
Examples included
- BooleanToYNTransformer (Y/N from boolean)
- FormatPhoneTransformer (strip non‑digits)

Drop‑in custom transformers
- Build your transformer DLL targeting .NET compatible with the host.
- Place it in the configured Transformers directory; the app logs loaded transformer types at startup.

---

## Usage examples
Trigger a configured mapping (example VendorIntegration)
```bash
curl -X POST \
 -H "Content-Type: application/json" \
 --data @QuickApiMapper.UnitTests/Test_Data/VendorIntegration/VendorIntegration-Input.json \
 http://localhost:5072/VendorIntegration
```
Inspect API contract
```bash
curl http://localhost:5072/openapi/v1.json
```

---

## Security and secrets
- Don’t commit real credentials in appsettings.*. For local dev, prefer user‑secrets or environment variables.
- AuthenticationBehavior posts to a token endpoint; ensure TLS and store client secrets securely (KeyVault/Secret Manager).
- SOAP endpoints often require credentials passed via SOAP headers—use SoapConfig with static values sourced from secure configuration.

---

## Testing and quality gates
- Tests: `dotnet test` from repo root.
- Warnings are treated as errors (Directory.Build.props: TreatWarningsAsErrors=true); keep code clean to avoid CI breaks.
- Analyzers are enabled; follow code style rules.

---

## Troubleshooting
- 400 No XML/JSON output: ensure Mapping rules produce output for the selected destination.
- Unsupported source/destination type: verify SourceType/DestinationType values.
- Token acquisition failed: check token endpoint, client credentials, and networking.
- No handler found for destination type: ensure destination type is one of JSON|XML|SOAP.
- Namespaces in SOAP payload: if you see blank namespaces, use `SoapConfig.BodyWrapperFieldXPath` so the mapped XML root adopts the wrapper’s namespace.

Logging tips
- On startup the app logs: loaded transformers and configured mappings.
- For SOAP, outgoing envelopes are logged at Debug; enable Debug for deeper diagnostics.

---

## Extensibility
- Add a new transformer
 - Implement ITransformer and drop the DLL into the Transformers directory or wire it via DI (services.AddTransformer<T>()).
- Add a new behavior
 - Implement IPreRunBehavior/IPostRunBehavior/IWholeRunBehavior and register via DI or load from a directory with AddBehaviorsFromDirectory.
- Add a new destination handler
 - Implement IDestinationHandler (in Application/Destinations); register via DI and handle content‑type and forwarding.

---

## Production checklist
- Remove secrets from appsettings.*; use secure providers.
- Configure TLS, reverse proxies, and proper ASPNETCORE_URLS bindings.
- Ensure timeouts/retries via HttpClientConfigurationBehavior as appropriate.
- Monitor logs and add structured logging sinks as needed.
- Pin transformer/behavior DLL versions and validate integrity before loading.

---

## Contributing

We use **automated releases** based on **conventional commits**. All contributions should follow this workflow:

### Quick Start for Contributors

1. **Fork and clone** the repository
2. **Create a feature branch**: `git checkout -b feature/my-feature`
3. **Make changes and commit** using conventional commit format:
 ```bash
 git commit -m "feat: add new feature"
 git commit -m "fix: resolve bug"
 git commit -m "docs: update documentation"
 ```
4. **Push and create PR**: Our CI will validate your changes with a complete dry-run
5. **Merge to main**: Use squash merge with conventional commit message

### Conventional Commit Format

Use these prefixes to control versioning and releases:

| Prefix | Example | Effect |
|--------|---------|--------|
| `feat:` | `feat: add message capture` | **Minor release** (0.1.0 → 0.2.0) |
| `fix:` | `fix: resolve null reference` | **Patch release** (0.1.0 → 0.1.1) |
| `perf:` | `perf: optimize queries` | **Patch release** (0.1.0 → 0.1.1) |
| `feat!:` | `feat!: redesign API` | **Major release** (0.1.0 → 1.0.0) |
| `docs:` | `docs: update README` | No release (build only) |
| `chore:` | `chore: update deps` | No release (build only) |
| `test:` | `test: add unit tests` | No release (build only) |

**Automatic Release Process**:
1. Commit with `feat:` or `fix:` prefix and push to main
2. CI workflow builds, tests, and analyzes commits
3. Tag automatically created (e.g., `v0.2.0`)
4. Release workflow triggered and NuGet packages published

Manual tagging is not required. 

For detailed contribution guidelines, see [CONTRIBUTING.md](CONTRIBUTING.md).

For a quick reference, see [CI/CD Quick Reference](.github/QUICK_REFERENCE.md).

For complete workflow documentation, see [Workflows README](.github/workflows/README.md).

---

## Maintainer notes
- Key entry points: QuickApiMapper.Web/Program.cs and QuickApiMapper.Application/Extensions/ServiceCollectionExtensions.cs
- Mapping contracts: QuickApiMapper.Contracts (ApiMappingConfig, FieldMapping, Transformer, SoapConfig, etc.)
- Behavior examples and pipeline usage are well‑covered in tests; use them as references for expected semantics.
- If Scalar UI route changes, you still have /openapi/v1.json.

Internal documentation improvements welcome; keep README aligned with Program.cs and service registration.

