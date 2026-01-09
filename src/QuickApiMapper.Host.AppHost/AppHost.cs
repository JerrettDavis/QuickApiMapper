var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Services
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("quickapimapper-db");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Management API - Central configuration and administration
var managementApi = builder.AddProject<Projects.QuickApiMapper_Management_Api>("management-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithHttpHealthCheck("/health");

// Designer Web UI - Visual configuration interface
var designerWeb = builder.AddProject<Projects.QuickApiMapper_Designer_Web>("designer-web")
    .WithReference(managementApi)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(managementApi);

// Main QuickApiMapper Web API - Integration mappings service
var webApi = builder.AddProject<Projects.QuickApiMapper_Web>("web-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
