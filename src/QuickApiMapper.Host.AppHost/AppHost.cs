var builder = DistributedApplication.CreateBuilder(args);

// ===========================
// INFRASTRUCTURE SERVICES
// ===========================
// These must start first as they are dependencies for the application services

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("quickapimapper-db");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// ===========================
// DEMO SERVICES (STANDALONE)
// ===========================
// These demo services simulate external systems and have no dependencies
// They can start in parallel with infrastructure

// Demo JSON API - Modern E-commerce Order API
// Provides: REST/JSON endpoints for order submission and retrieval
// Endpoints: POST /api/orders, GET /api/orders, GET /api/orders/{id}, PUT /api/orders/{id}/status
// Used by: QuickApiMapper Web API for demonstration of JSON-to-SOAP transformations
var demoJsonApi = builder.AddProject<Projects.Demo_JsonApi>("demo-jsonapi")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// Demo SOAP API - Legacy Warehouse/ERP System
// Provides: SOAP 1.1/1.2 endpoints for fulfillment operations
// Endpoints: SubmitFulfillmentRequest, GetFulfillmentStatus, CancelFulfillment
// WSDL: /WarehouseService.asmx?wsdl
// Used by: QuickApiMapper Web API for demonstration of JSON-to-SOAP transformations
var demoSoapApi = builder.AddProject<Projects.Demo_SoapApi>("demo-soapapi")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

// ===========================
// MANAGEMENT API
// ===========================
// Central configuration and administration service
// Manages integration mappings, endpoints, and demo data

var managementApi = builder.AddProject<Projects.QuickApiMapper_Management_Api>("management-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(postgres)
    .WaitFor(redis);

// Enable demo mode in Development environment
// This automatically seeds the database with sample integration mappings
// that connect the Demo JSON API to the Demo SOAP API
if (builder.Environment.EnvironmentName == "Development")
{
    managementApi.WithEnvironment("DemoMode__EnableDemoMode", "true");
    managementApi.WithEnvironment("DemoMode__ForceReseed", "false");
    managementApi.WithEnvironment("DemoMode__SampleMessageCount", "15");
    managementApi.WithEnvironment("DemoMode__FailedMessageCount", "3");
}

// ===========================
// MAIN WEB API
// ===========================
// The primary QuickApiMapper service that performs integration mappings
// This service references the demo services for service discovery

var webApi = builder.AddProject<Projects.QuickApiMapper_Web>("web-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    // Service discovery references to demo services
    // These allow the Web API to discover and call the demo services
    .WithReference(demoJsonApi)
    .WithReference(demoSoapApi)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(managementApi)
    .WaitFor(demoJsonApi)
    .WaitFor(demoSoapApi);

// ===========================
// DESIGNER WEB UI
// ===========================
// Visual configuration interface for creating and managing integration mappings

var designerWeb = builder.AddProject<Projects.QuickApiMapper_Designer_Web>("designer-web")
    .WithReference(managementApi)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(managementApi);

// ===========================
// STARTUP INFORMATION
// ===========================
// When you run this AppHost with 'dotnet run', the following services will start:
//
// 1. Infrastructure (parallel):
//    - PostgreSQL (with PgAdmin UI)
//    - Redis
//    - RabbitMQ (with Management UI)
//
// 2. Demo Services (parallel):
//    - Demo JSON API (e-commerce order API)
//    - Demo SOAP API (warehouse/ERP system)
//
// 3. Management API (after infrastructure):
//    - Manages integration mappings
//    - Auto-seeds demo data in Development
//
// 4. Main Services (after dependencies):
//    - QuickApiMapper Web API (after Management API + Demo services)
//    - Designer Web UI (after Management API)
//
// DATA FLOW IN DEMO MODE:
// 1. Management API seeds demo integration mappings connecting JSON API → SOAP API
// 2. Demo JSON API receives modern REST/JSON requests
// 3. QuickApiMapper Web API transforms JSON → SOAP
// 4. Demo SOAP API processes legacy SOAP requests
// 5. Responses flow back: SOAP → JSON → Client
//
// ACCESS POINTS:
// - Aspire Dashboard: https://localhost:15001 (or as configured)
// - Demo JSON API: Check Aspire Dashboard for assigned port
// - Demo SOAP API: Check Aspire Dashboard for assigned port (WSDL at /WarehouseService.asmx?wsdl)
// - Management API: Check Aspire Dashboard for assigned port
// - Web API: Check Aspire Dashboard for assigned port
// - Designer Web: Check Aspire Dashboard for assigned port

builder.Build().Run();
