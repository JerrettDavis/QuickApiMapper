using Demo.JsonApi.Models;
using Demo.JsonApi.Services;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, service discovery)
builder.AddServiceDefaults();

// Add OpenAPI services
builder.Services.AddOpenApi();

// Register order service
builder.Services.AddSingleton<IOrderService, InMemoryOrderService>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure Scalar for OpenAPI documentation
app.MapScalarApiReference();

// Use HTTPS redirection
app.UseHttpsRedirection();

// API Endpoints

/// <summary>
/// Submit a new order
/// </summary>
app.MapPost("/api/orders", async (
    [FromBody] Order order,
    IOrderService orderService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var createdOrder = await orderService.CreateOrderAsync(order, cancellationToken);
        return Results.Created($"/api/orders/{createdOrder.OrderId}", createdOrder);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Error creating order");
    }
})
.WithName("CreateOrder")
.WithSummary("Submit a new order")
.WithDescription("Creates a new e-commerce order and returns the created order details")
.WithTags("Orders")
.Produces<Order>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict)
.Produces(StatusCodes.Status500InternalServerError);

/// <summary>
/// Get all orders
/// </summary>
app.MapGet("/api/orders", async (
    IOrderService orderService,
    CancellationToken cancellationToken) =>
{
    var orders = await orderService.GetAllOrdersAsync(cancellationToken);
    return Results.Ok(orders);
})
.WithName("GetAllOrders")
.WithSummary("List all orders")
.WithDescription("Retrieves all orders ordered by date (newest first)")
.WithTags("Orders")
.Produces<IEnumerable<Order>>(StatusCodes.Status200OK);

/// <summary>
/// Get a specific order by ID
/// </summary>
app.MapGet("/api/orders/{id}", async (
    string id,
    IOrderService orderService,
    CancellationToken cancellationToken) =>
{
    var order = await orderService.GetOrderByIdAsync(id, cancellationToken);

    if (order == null)
    {
        return Results.NotFound(new { error = $"Order with ID '{id}' not found" });
    }

    return Results.Ok(order);
})
.WithName("GetOrderById")
.WithSummary("Get a specific order")
.WithDescription("Retrieves a single order by its unique identifier")
.WithTags("Orders")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

/// <summary>
/// Update order status
/// </summary>
app.MapPut("/api/orders/{id}/status", async (
    string id,
    [FromBody] OrderStatusUpdate statusUpdate,
    IOrderService orderService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var order = await orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, cancellationToken);

    if (order == null)
    {
        return Results.NotFound(new { error = $"Order with ID '{id}' not found" });
    }

    if (!string.IsNullOrWhiteSpace(statusUpdate.Notes))
    {
        logger.LogInformation("Order {OrderId} status updated to {Status}. Notes: {Notes}",
            SanitizeForLog(id), statusUpdate.Status, SanitizeForLog(statusUpdate.Notes));
    }

    return Results.Ok(order);
})
.WithName("UpdateOrderStatus")
.WithSummary("Update order status")
.WithDescription("Updates the status of an existing order (Pending, Processing, Shipped, Delivered, Cancelled)")
.WithTags("Orders")
.Produces<Order>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

/// <summary>
/// Health check endpoint
/// </summary>
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    service = "Demo.JsonApi",
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithSummary("Health check")
.WithDescription("Returns the health status of the API")
.WithTags("Health")
.Produces(StatusCodes.Status200OK)
.ExcludeFromDescription();

// Map Aspire default endpoints (includes /health, /alive, /ready endpoints)
app.MapDefaultEndpoints();

app.Run();

/// <summary>
/// Removes CR/LF from user-supplied strings before logging to prevent log-forging.
/// </summary>
static string SanitizeForLog(string? value) =>
    value?.Replace("\r", "\\r", StringComparison.Ordinal)
          .Replace("\n", "\\n", StringComparison.Ordinal) ?? string.Empty;
