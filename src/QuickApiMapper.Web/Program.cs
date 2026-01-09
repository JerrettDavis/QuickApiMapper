using System.Xml.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;
using QuickApiMapper;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Contracts;
using QuickApiMapper.HealthChecks;
using QuickApiMapper.MessageCapture.InMemory.Extensions;
using Scalar.AspNetCore;
using FieldMapping = QuickApiMapper.Contracts.FieldMapping;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, service discovery)
builder.AddServiceDefaults();

// Use the new centralized service registration with provider pattern
var transformerDirectory = builder.Configuration.GetValue<string>("Transformers:Directory") ?? "Transformers";

// Register QuickApiMapper with file-based configuration (backward compatible)
// This can be easily switched to database-backed configuration by changing to:
// builder.Services.AddQuickApiMapperWithPersistence(options => { options.UseDatabase = true; ... })
builder.Services.AddQuickApiMapper(
    logging => logging.SetMinimumLevel(LogLevel.Information),
    transformerDirectory: transformerDirectory);

// Add file-based configuration provider (default for backward compatibility)
builder.Services.AddFileBasedConfiguration();

// Optionally add caching for better performance
builder.Services.AddCachedConfiguration(TimeSpan.FromMinutes(5));

// Add custom health checks for QuickApiMapper
builder.Services.AddHealthChecks()
    .AddCheck<ConfigurationProviderHealthCheck>("configuration_provider")
    .AddCheck<TransformersHealthCheck>("transformers");

// Add message capture (Phase 2 implementation)
builder.Services.AddInMemoryMessageCapture(options =>
{
    options.MaxPayloadSizeKB = 2048; // 2MB max payload size
    options.RetentionPeriod = TimeSpan.FromDays(7); // Keep messages for 7 days
});

// Register message capture behavior
builder.Services.AddSingleton<QuickApiMapper.Contracts.IWholeRunBehavior, QuickApiMapper.Behaviors.MessageCaptureBehavior>();

var app = builder.Build();

// Load integrations from the provider and log startup information
var configProvider = app.Services.GetRequiredService<IIntegrationConfigurationProvider>();
var integrations = await configProvider.GetAllActiveIntegrationsAsync();
var integrationList = integrations.ToList();

// Log configuration and transformers on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // Log loaded transformers
    var transformers = scope.ServiceProvider.GetServices<ITransformer>();
    var transformerNames = transformers.Select(t => t.GetType().FullName).ToList();
    logger.LogInformation("Loaded {Count} transformers: {Transformers}",
        transformerNames.Count, string.Join(", ", transformerNames));

    // Log configured mappings from provider
    logger.LogInformation("Loaded {Count} integration mappings from configuration provider: {Mappings}",
        integrationList.Count,
        string.Join(", ", integrationList.Select(m => m.Name)));
}

app.MapScalarApiReference();
app.UseHttpsRedirection();

// Create OpenAPI generator from loaded integrations
var namespaces = await configProvider.GetNamespacesAsync();
var staticValues = await configProvider.GetGlobalStaticValuesAsync();
var apiMappingConfig = new ApiMappingConfig(
    namespaces.Count > 0 ? namespaces : null,
    integrationList,
    staticValues.Count > 0 ? staticValues : null);
var openApiGenerator = new OpenApiDocumentGenerator(apiMappingConfig);


async Task<IResult> HandleMappingRequest(
    IntegrationMapping integration,
    HttpRequest request,
    HttpResponse response,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
{
    var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MappingHandler");
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

    // Check if input is enabled for this integration
    if (!integration.EnableInput)
    {
        logger.LogWarning("Input disabled for integration {Integration}", integration.Name);
        return Results.StatusCode(503); // Service Unavailable
    }

    try
    {
        logger.LogInformation("Processing {Integration} request to {Endpoint}",
            integration.Name, integration.Endpoint);

        // Read and parse input
        var inputBody = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
        logger.LogDebug("Received input: {Input}", inputBody);

        // Parse input and create appropriate mapping engine based on source and destination types
        var mappingEngineFactory = serviceProvider.GetRequiredService<IMappingEngineFactory>();

        if (integration.SourceType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
        {
            var inputJson = JObject.Parse(inputBody);

            if (integration.DestinationType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                // JSON to JSON mapping
                var outputJson = new JObject();
                var engine = mappingEngineFactory.CreateEngine<JObject, JObject>();

                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputJson,
                    outputJson,
                    integration.StaticValues,
                    apiMappingConfig.StaticValues,
                    serviceProvider,
                    cancellationToken);

                await HandleJsonDestination(integration, outputJson, request, response, httpClientFactory, serviceProvider, cancellationToken);
            }
            else if (integration.DestinationType.Equals("XML", StringComparison.OrdinalIgnoreCase) ||
                     integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase))
            {
                // JSON to XML mapping
                var outputXml = CreateXmlDocument(integration);
                var engine = mappingEngineFactory.CreateEngine<JObject, XDocument>();

                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputJson,
                    outputXml,
                    integration.StaticValues,
                    apiMappingConfig.StaticValues,
                    serviceProvider,
                    cancellationToken);

                await HandleXmlDestination(integration, outputXml, request, response, httpClientFactory, serviceProvider, cancellationToken);
            }
        }
        else if (integration.SourceType.Equals("XML", StringComparison.OrdinalIgnoreCase) ||
                 integration.SourceType.Equals("SOAP", StringComparison.OrdinalIgnoreCase))
        {
            var inputXml = XDocument.Parse(inputBody);

            if (integration.DestinationType.Equals("JSON", StringComparison.OrdinalIgnoreCase))
            {
                // XML to JSON mapping
                var outputJson = new JObject();
                var engine = mappingEngineFactory.CreateEngine<XDocument, JObject>();

                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputXml,
                    outputJson,
                    integration.StaticValues,
                    apiMappingConfig.StaticValues,
                    serviceProvider,
                    cancellationToken);

                await HandleJsonDestination(integration, outputJson, request, response, httpClientFactory, serviceProvider, cancellationToken);
            }
            else if (integration.DestinationType.Equals("XML", StringComparison.OrdinalIgnoreCase) ||
                     integration.DestinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase))
            {
                // XML to XML mapping
                var outputXml = CreateXmlDocument(integration);
                var engine = mappingEngineFactory.CreateEngine<XDocument, XDocument>();

                await engine.ApplyMappingAsync(
                    integration.Mapping ?? new List<FieldMapping>(),
                    inputXml,
                    outputXml,
                    integration.StaticValues,
                    apiMappingConfig.StaticValues,
                    serviceProvider,
                    cancellationToken);

                await HandleXmlDestination(integration, outputXml, request, response, httpClientFactory, serviceProvider, cancellationToken);
            }
        }
        else
        {
            logger.LogError("Unsupported source type: {SourceType}", integration.SourceType);
            return Results.BadRequest($"Unsupported source type: {integration.SourceType}");
        }

        logger.LogInformation("Successfully processed {Integration} request", integration.Name);
        return Results.Empty;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing {Integration} request", integration.Name);
        return Results.Problem($"Internal server error: {ex.Message}");
    }
}

XDocument CreateXmlDocument(IntegrationMapping integration)
{
    // Create XML output with proper namespace if specified
    var rootElementName = "root";
    XNamespace rootNamespace = "";

    // Use TNS namespace from static values if available
    if (integration.StaticValues?.TryGetValue("TnsNamespace", out var tnsNamespace) == true)
    {
        rootNamespace = tnsNamespace;
    }

    return new XDocument(new XElement(rootNamespace + rootElementName));
}

async Task HandleJsonDestination(
    IntegrationMapping integration,
    JObject outputJson,
    HttpRequest request,
    HttpResponse response,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
{
    // Check if output is enabled for this integration
    if (!integration.EnableOutput)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MappingHandler");
        logger.LogInformation("Output disabled for integration {Integration}, message captured but not forwarded", integration.Name);
        return; // Message was captured by MessageCaptureBehavior, but don't forward
    }

    var destinationHandlers = serviceProvider.GetServices<IDestinationHandler>();
    var handler = destinationHandlers.FirstOrDefault(h => h.CanHandle(integration.DestinationType)) ??
                  throw new InvalidOperationException($"No handler found for destination type: {integration.DestinationType}");
    await handler.HandleAsync(integration, outputJson, null, request, response, httpClientFactory, cancellationToken);
}

async Task HandleXmlDestination(
    IntegrationMapping integration,
    XDocument outputXml,
    HttpRequest request,
    HttpResponse response,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken)
{
    // Check if output is enabled for this integration
    if (!integration.EnableOutput)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MappingHandler");
        logger.LogInformation("Output disabled for integration {Integration}, message captured but not forwarded", integration.Name);
        return; // Message was captured by MessageCaptureBehavior, but don't forward
    }

    var destinationHandlers = serviceProvider.GetServices<IDestinationHandler>();
    var handler = destinationHandlers.FirstOrDefault(h => h.CanHandle(integration.DestinationType));

    if (handler == null)
    {
        throw new InvalidOperationException($"No handler found for destination type: {integration.DestinationType}");
    }

    await handler.HandleAsync(integration, null, outputXml, request, response, httpClientFactory, cancellationToken);
}

// Dynamically map endpoints for each integration loaded from the provider
foreach (var integration in integrationList)
{
    app.MapPost(integration.Endpoint, async (HttpRequest req, HttpResponse resp, IServiceProvider sp, CancellationToken ct) =>
            await HandleMappingRequest(integration, req, resp, sp, ct))
        .WithName(integration.Name)
        .WithSummary($"Process {integration.Name} integration")
        .WithDescription(
            $"Transforms {integration.SourceType} input to {integration.DestinationType} format and forwards to {integration.DestinationUrl}");
}

// Serve OpenAPI document
var openApiDoc = openApiGenerator.GenerateOpenApiDocument();
app.MapGet("/openapi/v1.json", () => Results.Content(openApiDoc.ToString(), "application/json"))
    .WithName("OpenApiDocument")
    .WithSummary("Get OpenAPI specification")
    .WithDescription("Returns the OpenAPI specification for all configured integrations");

// Map Aspire health check endpoints
app.MapDefaultEndpoints();

app.Run();