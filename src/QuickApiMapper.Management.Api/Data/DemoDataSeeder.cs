using Microsoft.Extensions.Options;
using QuickApiMapper.Management.Contracts.Models;
using QuickApiMapper.Management.Api.Services;

namespace QuickApiMapper.Management.Api.Data;

/// <summary>
/// Seeds the database with demo data for QuickApiMapper demonstrations.
/// </summary>
public class DemoDataSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly DemoModeOptions _options;
    private readonly IHostEnvironment _environment;

    public DemoDataSeeder(
        IServiceProvider serviceProvider,
        ILogger<DemoDataSeeder> logger,
        IOptions<DemoModeOptions> options,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _environment = environment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Only seed in Development environment when demo mode is enabled
        if (!_environment.IsDevelopment() || !_options.EnableDemoMode)
        {
            _logger.LogInformation("Demo mode is disabled or not in Development environment. Skipping demo data seeding.");
            return;
        }

        _logger.LogInformation("Demo mode enabled. Seeding demo data...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

            // Check if demo data already exists
            var existingIntegrations = await integrationService.GetAllAsync(cancellationToken);
            var hasDemoData = existingIntegrations.Any(i => i.Name.StartsWith("Demo:"));

            if (hasDemoData && !_options.ForceReseed)
            {
                _logger.LogInformation("Demo data already exists. Skipping seeding. Set ForceReseed to true to override.");
                return;
            }

            if (hasDemoData && _options.ForceReseed)
            {
                _logger.LogInformation("Force reseed enabled. Removing existing demo integrations...");
                foreach (var integration in existingIntegrations.Where(i => i.Name.StartsWith("Demo:")))
                {
                    await integrationService.DeleteAsync(integration.Id, cancellationToken);
                }
            }

            // Seed demo integrations
            await SeedDemoIntegrationsAsync(integrationService, cancellationToken);

            _logger.LogInformation("Demo data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo data: {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SeedDemoIntegrationsAsync(IIntegrationService integrationService, CancellationToken cancellationToken)
    {
        var demoIntegrations = GetDemoIntegrations();

        foreach (var integration in demoIntegrations)
        {
            try
            {
                _logger.LogInformation("Creating demo integration: {Name}", integration.Name);
                await integrationService.CreateAsync(integration, cancellationToken);
                _logger.LogInformation("Successfully created: {Name}", integration.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create demo integration {Name}: {Message}", integration.Name, ex.Message);
            }
        }
    }

    private static List<CreateIntegrationRequest> GetDemoIntegrations()
    {
        return new List<CreateIntegrationRequest>
        {
            // Integration 1: JSON to SOAP Order Processing (Main Demo)
            new CreateIntegrationRequest
            {
                Name = "Demo: JSON to SOAP Order Processing",
                Endpoint = "/api/demo/fulfillment/submit",
                SourceType = "JSON",
                DestinationType = "SOAP",
                DestinationUrl = "http://demo-soapapi/WarehouseService.asmx",
                IsActive = true,
                EnableInput = true,
                EnableOutput = true,
                EnableMessageCapture = true,
                FieldMappings = new List<FieldMappingDto>
                {
                    // Order identification
                    new FieldMappingDto
                    {
                        Source = "$.orderId",
                        Destination = "/OrderNumber",
                        Order = 1
                    },

                    // Customer information
                    new FieldMappingDto
                    {
                        Source = "$.customerName",
                        Destination = "/CustomerInfo/Name",
                        Order = 2
                    },
                    new FieldMappingDto
                    {
                        Source = "$.customerEmail",
                        Destination = "/CustomerInfo/ContactEmail",
                        Order = 3,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto { Name = "ToLower", Order = 1 }
                        }
                    },

                    // Order details
                    new FieldMappingDto
                    {
                        Source = "$.orderDate",
                        Destination = "/OrderDateTime",
                        Order = 4
                    },
                    new FieldMappingDto
                    {
                        Source = "$.totalAmount",
                        Destination = "/TotalValue",
                        Order = 5
                    },
                    new FieldMappingDto
                    {
                        Source = "$.currency",
                        Destination = "/CurrencyCode",
                        Order = 6
                    },

                    // Line items (array mapping)
                    new FieldMappingDto
                    {
                        Source = "$.items[*].sku",
                        Destination = "/LineItems/Item/SKU",
                        Order = 7,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto { Name = "ToUpper", Order = 1 }
                        }
                    },
                    new FieldMappingDto
                    {
                        Source = "$.items[*].productName",
                        Destination = "/LineItems/Item/Description",
                        Order = 8
                    },
                    new FieldMappingDto
                    {
                        Source = "$.items[*].quantity",
                        Destination = "/LineItems/Item/Qty",
                        Order = 9
                    },
                    new FieldMappingDto
                    {
                        Source = "$.items[*].unitPrice",
                        Destination = "/LineItems/Item/Price",
                        Order = 10
                    },

                    // Shipping address
                    new FieldMappingDto
                    {
                        Source = "$.shippingAddress.street",
                        Destination = "/DeliveryAddress/AddressLine1",
                        Order = 11
                    },
                    new FieldMappingDto
                    {
                        Source = "$.shippingAddress.city",
                        Destination = "/DeliveryAddress/City",
                        Order = 12
                    },
                    new FieldMappingDto
                    {
                        Source = "$.shippingAddress.state",
                        Destination = "/DeliveryAddress/StateProvince",
                        Order = 13
                    },
                    new FieldMappingDto
                    {
                        Source = "$.shippingAddress.postalCode",
                        Destination = "/DeliveryAddress/PostalCode",
                        Order = 14
                    },
                    new FieldMappingDto
                    {
                        Source = "$.shippingAddress.country",
                        Destination = "/DeliveryAddress/CountryCode",
                        Order = 15
                    },

                    // Priority with custom transformer
                    new FieldMappingDto
                    {
                        Source = "$.priority",
                        Destination = "/PriorityCode",
                        Order = 16,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto
                            {
                                Name = "MapValue",
                                Order = 1,
                                Arguments = new Dictionary<string, object>
                                {
                                    { "STANDARD", "STD" },
                                    { "EXPRESS", "EXP" },
                                    { "OVERNIGHT", "OVN" },
                                    { "default", "STD" }
                                }
                            }
                        }
                    }
                },
                SoapConfig = new SoapConfigDto
                {
                    BodyWrapperFieldXpath = "SubmitFulfillmentRequest",
                    Fields = new List<SoapFieldDto>
                    {
                        new SoapFieldDto
                        {
                            FieldType = "Header",
                            Xpath = "Action",
                            Source = "$$.SoapAction",
                            Order = 1
                        },
                        new SoapFieldDto
                        {
                            FieldType = "Body",
                            Xpath = "SubmitFulfillmentRequest",
                            Namespace = "http://warehouse.example.com/",
                            Order = 2
                        }
                    }
                },
                StaticValues = new Dictionary<string, string>
                {
                    { "SoapAction", "http://warehouse.example.com/SubmitFulfillmentRequest" },
                    { "SoapNamespace", "http://warehouse.example.com/" },
                    { "Version", "1.0" }
                }
            },

            // Integration 2: SOAP to JSON Status Updates
            new CreateIntegrationRequest
            {
                Name = "Demo: SOAP to JSON Fulfillment Status",
                Endpoint = "/api/demo/fulfillment/status",
                SourceType = "SOAP",
                DestinationType = "JSON",
                DestinationUrl = "http://demo-jsonapi/api/orders/status",
                IsActive = true,
                EnableInput = true,
                EnableOutput = true,
                EnableMessageCapture = true,
                FieldMappings = new List<FieldMappingDto>
                {
                    new FieldMappingDto
                    {
                        Source = "/FulfillmentStatusResponse/OrderNumber",
                        Destination = "$.orderId",
                        Order = 1
                    },
                    new FieldMappingDto
                    {
                        Source = "/FulfillmentStatusResponse/Status",
                        Destination = "$.status",
                        Order = 2,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto { Name = "ToLower", Order = 1 }
                        }
                    },
                    new FieldMappingDto
                    {
                        Source = "/FulfillmentStatusResponse/TrackingNumber",
                        Destination = "$.trackingNumber",
                        Order = 3
                    },
                    new FieldMappingDto
                    {
                        Source = "/FulfillmentStatusResponse/EstimatedDelivery",
                        Destination = "$.estimatedDeliveryDate",
                        Order = 4
                    },
                    new FieldMappingDto
                    {
                        Source = "/FulfillmentStatusResponse/LastUpdated",
                        Destination = "$.lastUpdated",
                        Order = 5
                    }
                },
                SoapConfig = new SoapConfigDto
                {
                    BodyWrapperFieldXpath = "FulfillmentStatusResponse"
                }
            },

            // Integration 3: RabbitMQ Order Processing
            new CreateIntegrationRequest
            {
                Name = "Demo: RabbitMQ Order Batch Processing",
                Endpoint = "/api/demo/batch/orders",
                SourceType = "JSON",
                DestinationType = "SOAP",
                DestinationUrl = "http://demo-soapapi/WarehouseService.asmx",
                IsActive = true,
                EnableInput = true,
                EnableOutput = true,
                EnableMessageCapture = true,
                FieldMappings = new List<FieldMappingDto>
                {
                    // Same mappings as Integration 1 for consistency
                    new FieldMappingDto { Source = "$.orderId", Destination = "/OrderNumber", Order = 1 },
                    new FieldMappingDto { Source = "$.customerName", Destination = "/CustomerInfo/Name", Order = 2 },
                    new FieldMappingDto
                    {
                        Source = "$.customerEmail",
                        Destination = "/CustomerInfo/ContactEmail",
                        Order = 3,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto { Name = "ToLower", Order = 1 }
                        }
                    },
                    new FieldMappingDto { Source = "$.orderDate", Destination = "/OrderDateTime", Order = 4 },
                    new FieldMappingDto { Source = "$.totalAmount", Destination = "/TotalValue", Order = 5 },
                    new FieldMappingDto { Source = "$.currency", Destination = "/CurrencyCode", Order = 6 },
                    new FieldMappingDto
                    {
                        Source = "$.items[*].sku",
                        Destination = "/LineItems/Item/SKU",
                        Order = 7,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto { Name = "ToUpper", Order = 1 }
                        }
                    },
                    new FieldMappingDto { Source = "$.items[*].productName", Destination = "/LineItems/Item/Description", Order = 8 },
                    new FieldMappingDto { Source = "$.items[*].quantity", Destination = "/LineItems/Item/Qty", Order = 9 },
                    new FieldMappingDto { Source = "$.items[*].unitPrice", Destination = "/LineItems/Item/Price", Order = 10 },
                    new FieldMappingDto { Source = "$.shippingAddress.street", Destination = "/DeliveryAddress/AddressLine1", Order = 11 },
                    new FieldMappingDto { Source = "$.shippingAddress.city", Destination = "/DeliveryAddress/City", Order = 12 },
                    new FieldMappingDto { Source = "$.shippingAddress.state", Destination = "/DeliveryAddress/StateProvince", Order = 13 },
                    new FieldMappingDto { Source = "$.shippingAddress.postalCode", Destination = "/DeliveryAddress/PostalCode", Order = 14 },
                    new FieldMappingDto { Source = "$.shippingAddress.country", Destination = "/DeliveryAddress/CountryCode", Order = 15 },
                    new FieldMappingDto
                    {
                        Source = "$.priority",
                        Destination = "/PriorityCode",
                        Order = 16,
                        Transformers = new List<TransformerDto>
                        {
                            new TransformerDto
                            {
                                Name = "MapValue",
                                Order = 1,
                                Arguments = new Dictionary<string, object>
                                {
                                    { "STANDARD", "STD" },
                                    { "EXPRESS", "EXP" },
                                    { "OVERNIGHT", "OVN" },
                                    { "default", "STD" }
                                }
                            }
                        }
                    }
                },
                SoapConfig = new SoapConfigDto
                {
                    BodyWrapperFieldXpath = "SubmitFulfillmentRequest",
                    Fields = new List<SoapFieldDto>
                    {
                        new SoapFieldDto
                        {
                            FieldType = "Header",
                            Xpath = "Action",
                            Source = "$$.SoapAction",
                            Order = 1
                        },
                        new SoapFieldDto
                        {
                            FieldType = "Body",
                            Xpath = "SubmitFulfillmentRequest",
                            Namespace = "http://warehouse.example.com/",
                            Order = 2
                        }
                    }
                },
                StaticValues = new Dictionary<string, string>
                {
                    { "SoapAction", "http://warehouse.example.com/SubmitFulfillmentRequest" },
                    { "SoapNamespace", "http://warehouse.example.com/" },
                    { "Source", "BATCH_PROCESSOR" }
                }
            }
        };
    }
}

/// <summary>
/// Configuration options for demo mode.
/// </summary>
public class DemoModeOptions
{
    public const string SectionName = "DemoMode";

    /// <summary>
    /// Enables or disables demo mode and automatic data seeding.
    /// </summary>
    public bool EnableDemoMode { get; set; } = false;

    /// <summary>
    /// Forces re-seeding of demo data even if it already exists.
    /// </summary>
    public bool ForceReseed { get; set; } = false;

    /// <summary>
    /// Number of sample message captures to generate per integration.
    /// </summary>
    public int SampleMessageCount { get; set; } = 10;

    /// <summary>
    /// Number of failed message samples to generate for error demonstration.
    /// </summary>
    public int FailedMessageCount { get; set; } = 3;
}
