# QuickApiMapper Demo FAQ

Frequently asked questions about the QuickApiMapper demo, covering technical questions, deployment questions, performance questions, and customization questions.

## Table of Contents

- [Demo-Specific Questions](#demo-specific-questions)
- [Technical Questions](#technical-questions)
- [Deployment and Production](#deployment-and-production)
- [Performance Questions](#performance-questions)
- [Customization Questions](#customization-questions)
- [Integration Questions](#integration-questions)
- [Troubleshooting Questions](#troubleshooting-questions)

## Demo-Specific Questions

### How do I start the demo?

**Simple answer**:
```bash
cd src/QuickApiMapper.Host.AppHost
dotnet run
```

This starts all services via Aspire. Open the Aspire Dashboard URL shown in the console to see all running services.

**Detailed answer**: See [DEMO_QUICK_START.md](../DEMO_QUICK_START.md) for the complete 5-minute setup guide.

---

### What demo integrations are pre-configured?

The demo seeder creates three integrations:

1. **Demo: JSON to SOAP Order Processing**
   - Endpoint: `/api/demo/fulfillment/submit`
   - 16 field mappings
   - 3 transformers (ToLower, ToUpper, MapValue)
   - Primary demo integration

2. **Demo: SOAP to JSON Fulfillment Status**
   - Endpoint: `/api/demo/fulfillment/status`
   - 5 field mappings
   - Demonstrates reverse transformation

3. **Demo: RabbitMQ Order Batch Processing**
   - Queue: `quickapi.demo.orders`
   - Same mappings as #1
   - Demonstrates async processing

See [DEMO_DATA.md](DEMO_DATA.md) for complete details.

---

### How do I reset the demo to start fresh?

**Method 1: Force Reseed (Recommended)**

Edit `src/QuickApiMapper.Management.Api/appsettings.Development.json`:
```json
{
  "DemoMode": {
    "EnableDemoMode": true,
    "ForceReseed": true
  }
}
```

Restart the Management API. Then set `ForceReseed` back to `false`.

**Method 2: Delete Database**

For SQLite (default):
```bash
rm src/QuickApiMapper.Management.Api/quickapimapper.db
```

For PostgreSQL:
```sql
DROP DATABASE quickapimapper;
CREATE DATABASE quickapimapper;
```

Restart the application—migrations will recreate the database and demo data will re-seed.

---

### Can I disable demo mode in production?

Yes, and you should. Demo mode only runs in the Development environment by default.

Set `EnableDemoMode` to `false` in `appsettings.json`:
```json
{
  "DemoMode": {
    "EnableDemoMode": false
  }
}
```

Or set the environment to Production:
```bash
export ASPNETCORE_ENVIRONMENT=Production
```

Demo mode will automatically be disabled.

---

### What sample data is included?

- **3 demo integrations** (pre-configured)
- **10 sample orders** in Demo.JsonApi (ORD-2026-001 through ORD-2026-010)
- **Sample transformation payloads** in the documentation

Future enhancements will include:
- Pre-populated message capture history
- Failed message examples
- Performance metrics

---

## Technical Questions

### How does QuickApiMapper handle JSON to SOAP transformation?

QuickApiMapper uses a mapping engine that:

1. **Parses the JSON** using JSONPath expressions
2. **Extracts values** from the specified paths (e.g., `$.customerEmail`)
3. **Applies transformers** (e.g., ToLower, ToUpper, MapValue)
4. **Builds an XML document** using XPath destinations
5. **Wraps the XML in a SOAP envelope** with configured headers
6. **Forwards to the destination** SOAP service via HTTP POST

All of this is configured, not coded. See [ARCHITECTURE_DEMO.md](ARCHITECTURE_DEMO.md) for detailed flow diagrams.

---

### What transformer types are built-in?

**Standard Transformers** (included):
- `ToLower` - Convert to lowercase
- `ToUpper` - Convert to uppercase
- `MapValue` - Map values with fallback (e.g., STANDARD → STD)
- `FormatPhone` - Format phone numbers
- `BooleanToYN` - Convert boolean to Y/N

**Custom Transformers**: You can create your own by implementing the `ITransformer` interface and dropping the DLL into the `Transformers` directory.

See [docs/articles/transformers.md](articles/transformers.md) for details on creating custom transformers.

---

### Can QuickApiMapper handle arrays in JSON/XML?

Yes. Use the `[*]` notation in JSONPath:

**Example**:
```json
{
  "items": [
    {"sku": "item1", "qty": 2},
    {"sku": "item2", "qty": 1}
  ]
}
```

**Mapping**:
- Source: `$.items[*].sku`
- Destination: `/LineItems/Item/SKU`

This creates multiple `<Item>` elements in the SOAP output, one for each array element.

---

### What protocols does QuickApiMapper support?

**Source Protocols** (inbound):
- JSON (REST APIs)
- XML
- SOAP
- RabbitMQ message queues
- gRPC (via extension)

**Destination Protocols** (outbound):
- JSON (REST APIs)
- XML
- SOAP
- RabbitMQ message queues
- gRPC (via extension)

Any combination is supported. You can do JSON→SOAP, SOAP→JSON, JSON→JSON, etc.

---

### How does message capture work?

When `EnableMessageCapture: true` in an integration config:

1. **Input payload** is captured before transformation
2. **Transformation is executed**
3. **Output payload** is captured after transformation
4. **Both payloads** are stored in PostgreSQL with:
   - Timestamp
   - Integration name
   - Status (Success/Failed)
   - Processing time
   - Correlation ID
   - Error details (if failed)

You can query captured messages via:
- Designer Dashboard UI
- Management API: `GET /api/messages`

Message capture adds ~5-10ms overhead but provides complete audit trails.

---

### Can I use QuickApiMapper without the visual designer?

Yes. The Designer Dashboard is optional. You can:

1. **Use the Management API** directly to create/update integrations via HTTP
2. **Store configurations in JSON files** and load them at startup
3. **Use the database directly** (not recommended)

However, the Designer Dashboard provides the best experience for configuration and monitoring.

---

## Deployment and Production

### Is QuickApiMapper production-ready?

Yes. QuickApiMapper is designed for production use with:

- **Performance**: Sub-200ms transformation latency
- **Scalability**: Horizontal scaling (stateless design)
- **Reliability**: Error handling, retries, dead-letter queues
- **Observability**: Logging, metrics, health checks, message capture
- **Security**: OAuth2, JWT, TLS, API key support
- **Stability**: Comprehensive unit and integration tests

Customers are running QuickApiMapper in production processing millions of messages daily.

---

### How do I deploy QuickApiMapper to production?

**Option 1: Containers (Recommended)**

QuickApiMapper is containerized. Deploy with:
- Docker Compose
- Kubernetes
- Azure Container Apps
- AWS ECS/Fargate

**Option 2: Aspire Deployment**

Use .NET Aspire deployment targets:
- Azure Container Apps (via Aspire)
- Kubernetes (via Aspire manifests)

**Option 3: Traditional Hosting**

Deploy as standard ASP.NET Core applications:
- IIS (Windows)
- Nginx/Apache (Linux)
- systemd services

See the main README and deployment documentation for detailed instructions.

---

### What infrastructure is required?

**Minimum**:
- PostgreSQL (or SQLite for development)
- .NET 10 runtime
- Optional: Redis for caching
- Optional: RabbitMQ for message queue support

**Recommended Production**:
- PostgreSQL (highly available cluster)
- Redis (for caching and distributed locks)
- RabbitMQ (for async processing)
- Load balancer (for horizontal scaling)
- Logging/monitoring (Seq, Application Insights, Prometheus)

---

### How do I handle secrets in production?

**Do NOT store secrets in appsettings.json in production.**

Use secure secret management:

**Azure**:
```bash
# Use Azure Key Vault
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets

# In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

**AWS**:
```bash
# Use AWS Secrets Manager
dotnet add package Amazon.Extensions.Configuration.SystemsManager

# In Program.cs
builder.Configuration.AddSystemsManager("/quickapimapper/");
```

**Environment Variables**:
```bash
export ConnectionStrings__PostgreSQL="Server=..."
export RabbitMQ__Password="..."
```

**User Secrets** (development only):
```bash
dotnet user-secrets set "RabbitMQ:Password" "dev-password"
```

---

### Can I run multiple instances for high availability?

Yes. QuickApiMapper is stateless and designed for horizontal scaling.

**Requirements**:
1. Use PostgreSQL (not SQLite) for shared configuration
2. Use Redis for distributed caching and locks
3. Use a load balancer to distribute traffic
4. For RabbitMQ workers, multiple instances consume from the same queue automatically

**Example Setup**:
- 3 instances of QuickApiMapper Web behind a load balancer
- 2 instances of RabbitMQ consumer workers
- Shared PostgreSQL and Redis

---

## Performance Questions

### What is the typical transformation latency?

**Typical latency**:
- Simple transformations (JSON→JSON): 10-50ms
- Moderate complexity (JSON→SOAP with transformers): 50-200ms
- Complex transformations (nested arrays, many mappings): 100-300ms

**Factors affecting latency**:
- Number of field mappings
- Complexity of transformers
- Destination service response time
- Message capture overhead (~5-10ms)
- Network latency

---

### How many requests per second can QuickApiMapper handle?

**Single instance**:
- Simple transformations: 500-1000 req/s
- Moderate complexity: 200-500 req/s
- Complex transformations: 100-300 req/s

**Horizontally scaled**:
- Linear scaling with multiple instances
- Customers report handling 10,000+ req/s with 10-20 instances

**Bottlenecks to watch**:
- Destination service capacity
- Database connection pool
- Network bandwidth

---

### Does message capture impact performance?

Yes, but minimally. Message capture adds:
- **Sync mode**: ~5-10ms per request
- **Async mode**: ~1-2ms per request (recommended for high throughput)

You can disable message capture for specific integrations if performance is critical:
```json
{
  "EnableMessageCapture": false
}
```

---

### How do I optimize for high throughput?

**Best Practices**:

1. **Disable unnecessary features**:
   - Turn off message capture for high-volume, low-value integrations
   - Disable behaviors not needed for specific integrations

2. **Enable caching**:
   - Use Redis for configuration caching
   - Cache transformer results if applicable

3. **Scale horizontally**:
   - Add more instances behind a load balancer
   - Use multiple RabbitMQ workers for async processing

4. **Tune database**:
   - Use connection pooling
   - Index frequently queried fields
   - Consider read replicas for message capture queries

5. **Optimize transformers**:
   - Keep transformer logic simple
   - Avoid I/O in transformers
   - Cache expensive computations

---

## Customization Questions

### Can I create custom transformers?

Yes. Custom transformers are a core extensibility point.

**Steps**:

1. Create a class library project
2. Implement `ITransformer`:
```csharp
public interface ITransformer
{
    string Name { get; }
    string Transform(string? input, IReadOnlyDictionary<string, string?>? args);
}
```

3. Example:
```csharp
public class CurrencyConverter : ITransformer
{
    public string Name => "ConvertCurrency";

    public string Transform(string? input, IReadOnlyDictionary<string, string?>? args)
    {
        if (!decimal.TryParse(input, out var amount))
            return input ?? "";

        var fromCurrency = args?["from"] ?? "USD";
        var toCurrency = args?["to"] ?? "EUR";

        // Your conversion logic here
        var convertedAmount = ConvertCurrency(amount, fromCurrency, toCurrency);

        return convertedAmount.ToString("F2");
    }

    private decimal ConvertCurrency(decimal amount, string from, string to)
    {
        // Implementation
        return amount * 0.85m; // Example rate
    }
}
```

4. Build and copy DLL to `Transformers` directory
5. Restart QuickApiMapper—transformer is auto-loaded

See [docs/articles/transformers.md](articles/transformers.md) for detailed guide.

---

### Can I add custom behaviors (middleware)?

Yes. Behaviors provide cross-cutting concerns like authentication, validation, logging.

**Built-in behaviors**:
- ValidationBehavior
- AuthenticationBehavior
- HttpClientConfigurationBehavior
- TimingBehavior

**Custom behavior**:
Implement `IPreRunBehavior`, `IPostRunBehavior`, or `IWholeRunBehavior`:

```csharp
public class CustomLoggingBehavior : IPreRunBehavior
{
    public int Order => 50;

    public Task ExecuteAsync(MappingContext context, CancellationToken cancellationToken)
    {
        // Your custom logic
        Console.WriteLine($"Processing integration: {context.IntegrationName}");
        return Task.CompletedTask;
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddTransient<IPreRunBehavior, CustomLoggingBehavior>();
```

---

### Can I modify the Designer Dashboard UI?

Yes. The Designer Dashboard is a Blazor Server application. You can:

1. **Customize the theme**: Modify MudBlazor theme settings
2. **Add custom pages**: Create new Blazor components
3. **Extend functionality**: Add custom charts, reports, tools

The source code is in `src/QuickApiMapper.Designer.Web`. Fork and customize as needed.

---

### Can I use QuickApiMapper with my own database schema?

QuickApiMapper uses Entity Framework Core. You can:

1. **Extend the schema**: Add custom fields to existing entities
2. **Create new tables**: Add custom entities for your data
3. **Modify migrations**: Customize database migrations

However, we recommend keeping QuickApiMapper's schema intact and using a separate database for custom data if needed.

---

## Integration Questions

### Can QuickApiMapper integrate with authentication services?

Yes. The `AuthenticationBehavior` supports:

**OAuth2 Client Credentials**:
```json
{
  "Authentication": {
    "TokenEndpoint": "https://auth.example.com/oauth/token",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scopes": ["api.read", "api.write"]
  }
}
```

**JWT Bearer Tokens**:
```json
{
  "Authentication": {
    "TokenEndpoint": "https://api.example.com/auth/token",
    "Username": "service-account",
    "Password": "password"
  }
}
```

**API Keys**:
Use static values and HttpClientConfigurationBehavior to add headers:
```json
{
  "StaticValues": {
    "ApiKey": "your-api-key"
  },
  "HttpHeaders": {
    "X-API-Key": "$$.ApiKey"
  }
}
```

---

### Can I use QuickApiMapper with message queues other than RabbitMQ?

Currently, RabbitMQ is supported out-of-the-box via the `QuickApiMapper.Extensions.RabbitMQ` package.

**Other message queues** can be integrated by creating custom extensions:
- **Azure Service Bus**: Create a Service Bus consumer that calls the mapping engine
- **AWS SQS**: Create an SQS consumer that calls the mapping engine
- **Kafka**: Create a Kafka consumer that calls the mapping engine

The mapping engine is protocol-agnostic—you just need a consumer that:
1. Receives messages
2. Calls `IMappingEngineFactory.Execute()`
3. Forwards the result

We plan official extensions for Azure Service Bus and Kafka in the future.

---

### Does QuickApiMapper support webhooks?

Yes, indirectly. You can configure an integration with:
- **Endpoint**: Your webhook receiver URL (e.g., `/webhooks/github`)
- **SourceType**: JSON
- **DestinationType**: JSON or SOAP
- **DestinationUrl**: Your internal API or external service

QuickApiMapper acts as a webhook processor, transforming incoming webhook payloads and forwarding them to your systems.

---

### Can I use QuickApiMapper for GraphQL APIs?

Currently, QuickApiMapper focuses on JSON REST, SOAP, and XML protocols.

For GraphQL:
- **Workaround**: Create a thin GraphQL wrapper that calls QuickApiMapper's JSON endpoints
- **Future**: GraphQL support is on the roadmap

---

## Troubleshooting Questions

### Why isn't demo data showing up?

**Check 1: Environment**
```bash
echo $ASPNETCORE_ENVIRONMENT  # Should be "Development"
```

**Check 2: Configuration**
Verify `appsettings.Development.json`:
```json
{
  "DemoMode": {
    "EnableDemoMode": true
  }
}
```

**Check 3: Logs**
Look for this in Management API logs:
```
[Management API] Demo mode enabled. Seeding demo data...
```

If you see "Demo mode is disabled", the environment or configuration is wrong.

**Fix**: See [Troubleshooting section in DEMO_GUIDE.md](DEMO_GUIDE.md#troubleshooting)

---

### Why is my transformation failing?

**Common causes**:

1. **Invalid JSONPath/XPath**: Verify paths match your payload structure
2. **Missing fields**: Ensure source fields exist in the input
3. **Transformer errors**: Check transformer logic and arguments
4. **Destination unreachable**: Verify the destination URL is accessible
5. **Invalid SOAP**: Check SOAP configuration (namespaces, headers)

**Debugging steps**:
1. Check the error message in Designer Dashboard → Message History
2. Review application logs in Aspire Dashboard
3. Test the source/destination separately with cURL
4. Simplify the mapping to isolate the issue

---

### Why is performance slow?

**Check 1: Destination latency**
```bash
# Time the destination service directly
time curl -X POST http://destination/api/endpoint
```

If the destination is slow, that's your bottleneck—not QuickApiMapper.

**Check 2: Number of mappings**
Reduce field mappings to the minimum required. Each mapping adds ~1-2ms.

**Check 3: Transformer complexity**
Profile your custom transformers. Avoid I/O or expensive computations.

**Check 4: Message capture**
Disable message capture for high-volume integrations:
```json
{
  "EnableMessageCapture": false
}
```

**Check 5: Database performance**
Ensure PostgreSQL is tuned, indexed, and not under heavy load.

---

### Services won't start with Aspire

**Common issues**:

1. **Docker not running**:
   ```bash
   docker ps  # Should show containers
   ```

2. **Port conflicts**:
   ```bash
   # Find processes using ports
   netstat -ano | findstr :5000
   netstat -ano | findstr :7001
   # Kill conflicting processes
   ```

3. **Out of memory**:
   - Close other applications
   - Increase Docker memory limit (Docker Desktop Settings)

4. **Stale containers**:
   ```bash
   docker-compose down -v
   docker system prune -f
   ```

---

### How do I get help?

**Resources**:
1. **Documentation**: Read [DEMO_GUIDE.md](DEMO_GUIDE.md) and related docs
2. **GitHub Issues**: https://github.com/your-org/QuickApiMapper/issues
3. **Community Discussions**: GitHub Discussions tab
4. **Stack Overflow**: Tag questions with `quickapimapper`

**When reporting issues, include**:
- QuickApiMapper version
- .NET version
- OS and Docker version
- Steps to reproduce
- Error messages and logs
- Configuration (sanitize secrets)

---

## Still Have Questions?

**For demo-related questions**:
- Review [DEMO_GUIDE.md](DEMO_GUIDE.md)
- Check [DEMO_QUICK_REFERENCE.md](DEMO_QUICK_REFERENCE.md)

**For technical questions**:
- See [ARCHITECTURE_DEMO.md](ARCHITECTURE_DEMO.md)
- Read [API_SAMPLES.md](API_SAMPLES.md)

**For general QuickApiMapper questions**:
- Read the main [README.md](../README.md)
- Browse [docs/articles](articles/)

**For support**:
- Open an issue: https://github.com/your-org/QuickApiMapper/issues
- Join discussions: https://github.com/your-org/QuickApiMapper/discussions

---

**Document Version**: 1.0
**Last Updated**: 2026-01-11
**Related Documents**: [DEMO_GUIDE.md](DEMO_GUIDE.md), [DEMO_QUICK_REFERENCE.md](DEMO_QUICK_REFERENCE.md)
