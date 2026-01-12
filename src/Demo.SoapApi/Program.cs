using Demo.SoapApi.Services;
using Demo.SoapApi.Storage;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, service discovery)
builder.AddServiceDefaults();

// Register SOAP service and repository
builder.Services.AddSingleton<IFulfillmentRepository, InMemoryFulfillmentRepository>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Map Aspire health check endpoints
app.MapDefaultEndpoints();

// Configure SOAP endpoint using IApplicationBuilder to avoid ambiguity
((IApplicationBuilder)app).UseSoapEndpoint<IWarehouseService>(
    path: "/WarehouseService.asmx",
    encoder: new SoapEncoderOptions(),
    serializer: SoapSerializer.DataContractSerializer);

// Add a simple home page with WSDL link
app.MapGet("/", () => Results.Content(
    """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Legacy Warehouse System - SOAP API</title>
        <style>
            body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
            .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            h1 { color: #333; border-bottom: 3px solid #4CAF50; padding-bottom: 10px; }
            h2 { color: #555; margin-top: 30px; }
            .info-box { background: #e8f5e9; padding: 15px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #4CAF50; }
            .endpoint { background: #f5f5f5; padding: 10px; border-radius: 4px; font-family: monospace; margin: 10px 0; }
            .operation { background: #fff3e0; padding: 10px; margin: 10px 0; border-radius: 4px; }
            a { color: #1976d2; text-decoration: none; font-weight: bold; }
            a:hover { text-decoration: underline; }
            ul { line-height: 1.8; }
            .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; font-size: 14px; }
        </style>
    </head>
    <body>
        <div class="container">
            <h1>🏭 Legacy Warehouse/ERP System</h1>
            <div class="info-box">
                <strong>SOAP Web Service Interface v2.0</strong><br>
                This is a simulated legacy enterprise warehouse management system that exposes SOAP endpoints for order fulfillment operations.
            </div>

            <h2>📋 WSDL Definition</h2>
            <p>Access the service definition at:</p>
            <div class="endpoint">
                <a href="/WarehouseService.asmx?wsdl">/WarehouseService.asmx?wsdl</a>
            </div>

            <h2>🔧 Available Operations</h2>

            <div class="operation">
                <strong>SubmitFulfillmentRequest</strong><br>
                Submit a new order fulfillment request to the warehouse system
            </div>

            <div class="operation">
                <strong>GetFulfillmentStatus</strong><br>
                Query the status of an existing fulfillment by order number or confirmation number
            </div>

            <div class="operation">
                <strong>CancelFulfillment</strong><br>
                Cancel a pending or processing fulfillment request
            </div>

            <h2>📦 Service Information</h2>
            <ul>
                <li><strong>Namespace:</strong> http://warehouse.example.com/</li>
                <li><strong>Service Endpoint:</strong> /WarehouseService.asmx</li>
                <li><strong>Protocol:</strong> SOAP 1.1 / SOAP 1.2</li>
                <li><strong>Encoding:</strong> UTF-8</li>
                <li><strong>Storage:</strong> In-Memory (Demo)</li>
            </ul>

            <h2>🎯 Integration Example</h2>
            <p>This SOAP service is designed to work with QuickApiMapper for JSON-to-SOAP transformations. Configure QuickApiMapper to:</p>
            <ul>
                <li>Accept modern JSON REST requests</li>
                <li>Transform to legacy SOAP format</li>
                <li>Forward to this warehouse system</li>
                <li>Return SOAP responses to clients</li>
            </ul>

            <div class="footer">
                Demo SOAP API for QuickApiMapper Integration Testing<br>
                Part of the QuickApiMapper Aspire Application
            </div>
        </div>
    </body>
    </html>
    """,
    "text/html"));

app.Run();
