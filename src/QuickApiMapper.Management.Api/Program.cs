using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Management.Contracts.Models;
using QuickApiMapper.Management.Api.Services;
using QuickApiMapper.MessageCapture.InMemory.Extensions;
using QuickApiMapper.Persistence.Abstractions.Repositories;
using QuickApiMapper.Persistence.PostgreSQL;
using QuickApiMapper.Persistence.PostgreSQL.Extensions;
using QuickApiMapper.Persistence.SQLite;
using QuickApiMapper.Persistence.SQLite.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, service discovery)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow all localhost origins in development
            policy.SetIsOriginAllowed(origin =>
                {
                    var uri = new Uri(origin);
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>()
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

// Register persistence layer
var usePostgres = builder.Configuration.GetValue<bool>("Persistence:UsePostgreSQL");
var connectionString = builder.Configuration.GetConnectionString("QuickApiMapper");

if (usePostgres && !string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddPostgreSqlPersistence(connectionString);
}
else
{
    // Default to SQLite for development
    var sqliteDbPath = builder.Configuration["Persistence:SQLite:DatabasePath"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "quickapimapper.db");

    // Format as a proper SQLite connection string
    var sqliteConnectionString = $"Data Source={sqliteDbPath}";

    Console.WriteLine($"[Management API] Using SQLite: {sqliteDbPath}");
    builder.Services.AddSqlitePersistence(sqliteConnectionString);
}

// Register QuickApiMapper core services (for testing functionality)
builder.Services.AddQuickApiMapperCore();
builder.Services.AddQuickApiMapperResolvers();
builder.Services.AddQuickApiMapperWriters();

// Load transformers from embedded assemblies
builder.Services.AddQuickApiMapperTransformers();

// Register in-memory message capture provider
builder.Services.AddInMemoryMessageCapture(options =>
{
    options.MaxPayloadSizeKB = 1024; // 1 MB
    options.RetentionPeriod = TimeSpan.FromDays(7);
});

// Register application services
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<ISchemaImportService, SchemaImportService>();
builder.Services.AddScoped<ITestingService, TestingService>();

// Add database health check
if (usePostgres && !string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "database", tags: ["ready"]);
}
else
{
    builder.Services.AddHealthChecks()
        .AddSqlite($"Data Source={builder.Configuration["Persistence:SQLite:DatabasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "quickapimapper.db")}",
            name: "database",
            tags: ["ready"]);
}

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Initialize database on startup
try
{
    using var scope = app.Services.CreateScope();
    if (usePostgres)
    {
        var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperDbContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("[Management API] PostgreSQL database migrated");
    }
    else
    {
        var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperSqliteDbContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("[Management API] SQLite database migrated");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[Management API] Error migrating database: {ex.Message}");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("QuickApiMapper Management API")
            .WithTheme(ScalarTheme.Mars);
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Map Aspire health check endpoints
app.MapDefaultEndpoints();

// Database migration endpoint (development only)
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/admin/migrate", async (IServiceProvider services) =>
    {
        try
        {
            var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();

            if (usePostgres)
            {
                var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperDbContext>();
                await context.Database.MigrateAsync();
                return Results.Ok(new { success = true, message = "PostgreSQL database migrated" });
            }
            else
            {
                var context = scope.ServiceProvider.GetRequiredService<QuickApiMapperSqliteDbContext>();
                await context.Database.MigrateAsync();
                return Results.Ok(new { success = true, message = "SQLite database migrated" });
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, error = ex.Message });
        }
    }).WithTags("Admin");

    app.MapPost("/api/admin/seed", async (IServiceProvider services) =>
    {
        try
        {
            var integrationService = services.GetRequiredService<IIntegrationService>();

            // Check if we already have data
            var existing = await integrationService.GetAllAsync(CancellationToken.None);
            if (existing.Any())
            {
                return Results.Ok(new { success = true, message = $"Database already has {existing.Count()} integration(s)", seeded = false });
            }

            // Create sample integrations for demonstration
            var sampleIntegrations = new[]
            {
                new CreateIntegrationRequest
                {
                    Name = "E-Commerce Order Fulfillment",
                    Endpoint = "/api/fulfillment/orders",
                    SourceType = "JSON",
                    DestinationType = "SOAP",
                    DestinationUrl = "https://warehouse-system.demo.local/soap/v2/fulfillment",
                    IsActive = true,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "$.orderId", Destination = "/envelope/body/orderNumber", Order = 1 },
                        new() { Source = "$.customer.email", Destination = "/envelope/body/customerEmail", Order = 2 },
                        new() { Source = "$.customer.name", Destination = "/envelope/body/customerName", Order = 3 },
                        new() { Source = "$.shippingAddress.street", Destination = "/envelope/body/address/street", Order = 4 },
                        new() { Source = "$.shippingAddress.city", Destination = "/envelope/body/address/city", Order = 5 },
                        new() { Source = "$.shippingAddress.postalCode", Destination = "/envelope/body/address/zipCode", Order = 6 },
                        new() { Source = "$.items[*].sku", Destination = "/envelope/body/lineItems/item/productCode", Order = 7 },
                        new() { Source = "$.items[*].quantity", Destination = "/envelope/body/lineItems/item/qty", Order = 8 },
                        new() { Source = "$$.fulfillmentPriority", Destination = "/envelope/body/priority", Order = 9 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "fulfillmentPriority", "STANDARD" },
                        { "warehouseId", "WH-001" },
                        { "processingStatus", "PENDING" }
                    }
                },
                new CreateIntegrationRequest
                {
                    Name = "Supplier Data Sync",
                    Endpoint = "/api/suppliers/sync",
                    SourceType = "JSON",
                    DestinationType = "SOAP",
                    DestinationUrl = "https://procurement-api.demo.local/soap/suppliers",
                    IsActive = true,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "$.supplierId", Destination = "/envelope/body/vendorCode", Order = 1 },
                        new() { Source = "$.companyName", Destination = "/envelope/body/businessName", Order = 2 },
                        new() { Source = "$.taxId", Destination = "/envelope/body/taxIdentifier", Order = 3 },
                        new() { Source = "$.contact.email", Destination = "/envelope/body/primaryEmail", Order = 4 },
                        new() { Source = "$.contact.phone", Destination = "/envelope/body/phoneNumber", Order = 5 },
                        new() { Source = "$.paymentTerms", Destination = "/envelope/body/terms", Order = 6 },
                        new() { Source = "$$.accountStatus", Destination = "/envelope/body/status", Order = 7 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "accountStatus", "ACTIVE" },
                        { "vendorType", "STANDARD" },
                        { "creditRating", "A" }
                    }
                },
                new CreateIntegrationRequest
                {
                    Name = "Product Catalog Integration",
                    Endpoint = "/api/catalog/products",
                    SourceType = "XML",
                    DestinationType = "JSON",
                    DestinationUrl = "https://catalog-api.demo.local/v1/products",
                    IsActive = true,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "/products/product/sku", Destination = "$.productCode", Order = 1 },
                        new() { Source = "/products/product/name", Destination = "$.title", Order = 2 },
                        new() { Source = "/products/product/description", Destination = "$.longDescription", Order = 3 },
                        new() { Source = "/products/product/price", Destination = "$.pricing.basePrice", Order = 4 },
                        new() { Source = "/products/product/category", Destination = "$.categoryPath", Order = 5 },
                        new() { Source = "/products/product/stock", Destination = "$.inventory.quantity", Order = 6 },
                        new() { Source = "/products/product/images/image", Destination = "$.media.images[*]", Order = 7 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "currency", "USD" },
                        { "status", "PUBLISHED" },
                        { "visibility", "PUBLIC" }
                    }
                },
                new CreateIntegrationRequest
                {
                    Name = "Shipping Label Generator",
                    Endpoint = "/api/shipping/labels",
                    SourceType = "JSON",
                    DestinationType = "JSON",
                    DestinationUrl = "https://logistics-api.demo.local/v2/labels",
                    IsActive = false,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "$.shipmentId", Destination = "$.trackingNumber", Order = 1 },
                        new() { Source = "$.fromAddress", Destination = "$.origin", Order = 2 },
                        new() { Source = "$.toAddress", Destination = "$.destination", Order = 3 },
                        new() { Source = "$.package.weight", Destination = "$.packageWeight", Order = 4 },
                        new() { Source = "$.package.dimensions", Destination = "$.packageSize", Order = 5 },
                        new() { Source = "$$.serviceLevel", Destination = "$.shippingMethod", Order = 6 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "serviceLevel", "GROUND" },
                        { "carrier", "FEDEX" },
                        { "labelFormat", "PDF" }
                    }
                },
                new CreateIntegrationRequest
                {
                    Name = "Invoice Processing Service",
                    Endpoint = "/api/accounting/invoices",
                    SourceType = "JSON",
                    DestinationType = "SOAP",
                    DestinationUrl = "https://finance-system.demo.local/soap/invoices",
                    IsActive = true,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "$.invoiceNumber", Destination = "/envelope/body/invoiceId", Order = 1 },
                        new() { Source = "$.billTo.customerId", Destination = "/envelope/body/accountNumber", Order = 2 },
                        new() { Source = "$.billTo.name", Destination = "/envelope/body/billingName", Order = 3 },
                        new() { Source = "$.invoiceDate", Destination = "/envelope/body/issueDate", Order = 4 },
                        new() { Source = "$.dueDate", Destination = "/envelope/body/paymentDue", Order = 5 },
                        new() { Source = "$.lineItems[*].description", Destination = "/envelope/body/items/item/desc", Order = 6 },
                        new() { Source = "$.lineItems[*].amount", Destination = "/envelope/body/items/item/price", Order = 7 },
                        new() { Source = "$.totalAmount", Destination = "/envelope/body/totalDue", Order = 8 },
                        new() { Source = "$$.currency", Destination = "/envelope/body/currencyCode", Order = 9 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "currency", "USD" },
                        { "terms", "NET30" },
                        { "invoiceType", "STANDARD" }
                    }
                },
                new CreateIntegrationRequest
                {
                    Name = "Customer Notification Service",
                    Endpoint = "/api/notifications/send",
                    SourceType = "JSON",
                    DestinationType = "JSON",
                    DestinationUrl = "https://messaging-api.demo.local/v1/notifications",
                    IsActive = true,
                    FieldMappings = new List<FieldMappingDto>
                    {
                        new() { Source = "$.userId", Destination = "$.recipient.customerId", Order = 1 },
                        new() { Source = "$.email", Destination = "$.recipient.emailAddress", Order = 2 },
                        new() { Source = "$.message.subject", Destination = "$.content.title", Order = 3 },
                        new() { Source = "$.message.body", Destination = "$.content.message", Order = 4 },
                        new() { Source = "$$.notificationType", Destination = "$.type", Order = 5 },
                        new() { Source = "$$.priority", Destination = "$.deliveryPriority", Order = 6 }
                    },
                    StaticValues = new Dictionary<string, string>
                    {
                        { "notificationType", "TRANSACTIONAL" },
                        { "priority", "NORMAL" },
                        { "channel", "EMAIL" }
                    }
                }
            };

            foreach (var request in sampleIntegrations)
            {
                await integrationService.CreateAsync(request, CancellationToken.None);
            }

            return Results.Ok(new { success = true, message = $"Seeded {sampleIntegrations.Length} sample integrations", seeded = true });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { success = false, error = ex.Message });
        }
    }).WithTags("Admin");
}

app.Run();
