using System.Collections.Concurrent;
using Demo.JsonApi.Models;

namespace Demo.JsonApi.Services;

/// <summary>
/// In-memory implementation of order service using concurrent dictionary
/// </summary>
public class InMemoryOrderService : IOrderService
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();
    private readonly ILogger<InMemoryOrderService> _logger;

    public InMemoryOrderService(ILogger<InMemoryOrderService> logger)
    {
        _logger = logger;
        SeedOrders();
    }

    public Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId))
        {
            order.OrderId = GenerateOrderId();
        }

        order.OrderDate = DateTime.UtcNow;
        order.Status = OrderStatus.Pending;

        if (!_orders.TryAdd(order.OrderId, order))
        {
            throw new InvalidOperationException($"Order with ID {order.OrderId} already exists");
        }

        _logger.LogInformation("Created new order: {OrderId}", SanitizeForLog(order.OrderId));
        return Task.FromResult(order);
    }

    public Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_orders.Values.OrderByDescending(o => o.OrderDate).AsEnumerable());
    }

    public Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<Order?> UpdateOrderStatusAsync(string orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return Task.FromResult<Order?>(null);
        }

        order.Status = status;
        _logger.LogInformation("Updated order {OrderId} status to {Status}", SanitizeForLog(orderId), status);
        return Task.FromResult<Order?>(order);
    }

    private void SeedOrders()
    {
        var seedOrders = new[]
        {
            new Order
            {
                OrderId = "ORD-2026-001",
                CustomerName = "John Smith",
                CustomerEmail = "john.smith@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                TotalAmount = 599.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "LAPTOP-XPS15", ProductName = "Dell XPS 15 Laptop", Quantity = 1, UnitPrice = 599.99m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "123 Main St",
                    City = "Seattle",
                    State = "WA",
                    PostalCode = "98101",
                    Country = "USA"
                },
                Priority = "STANDARD",
                Status = OrderStatus.Delivered
            },
            new Order
            {
                OrderId = "ORD-2026-002",
                CustomerName = "Jane Doe",
                CustomerEmail = "jane.doe@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-3),
                TotalAmount = 1299.97m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "PHONE-IP15", ProductName = "iPhone 15 Pro", Quantity = 1, UnitPrice = 999.99m },
                    new() { Sku = "CASE-IP15", ProductName = "iPhone 15 Pro Case", Quantity = 1, UnitPrice = 49.99m },
                    new() { Sku = "CHRG-USB-C", ProductName = "USB-C Fast Charger", Quantity = 1, UnitPrice = 249.99m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "456 Oak Avenue",
                    City = "Portland",
                    State = "OR",
                    PostalCode = "97201",
                    Country = "USA"
                },
                Priority = "EXPRESS",
                Status = OrderStatus.Shipped
            },
            new Order
            {
                OrderId = "ORD-2026-003",
                CustomerName = "Bob Johnson",
                CustomerEmail = "bob.johnson@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                TotalAmount = 449.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "HEADPHONE-WH1000", ProductName = "Sony WH-1000XM5 Headphones", Quantity = 1, UnitPrice = 399.99m },
                    new() { Sku = "CABLE-AUX", ProductName = "Premium Audio Cable", Quantity = 1, UnitPrice = 50.00m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "789 Pine Street",
                    City = "San Francisco",
                    State = "CA",
                    PostalCode = "94102",
                    Country = "USA"
                },
                Priority = "STANDARD",
                Status = OrderStatus.Processing
            },
            new Order
            {
                OrderId = "ORD-2026-004",
                CustomerName = "Alice Williams",
                CustomerEmail = "alice.williams@example.com",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                TotalAmount = 2499.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "TABLET-IPAD", ProductName = "iPad Pro 12.9\"", Quantity = 1, UnitPrice = 1299.99m },
                    new() { Sku = "PENCIL-APPLE", ProductName = "Apple Pencil 2nd Gen", Quantity = 1, UnitPrice = 129.99m },
                    new() { Sku = "KEYBOARD-MAGIC", ProductName = "Magic Keyboard", Quantity = 1, UnitPrice = 349.99m },
                    new() { Sku = "COVER-IPAD", ProductName = "iPad Pro Smart Cover", Quantity = 1, UnitPrice = 79.99m },
                    new() { Sku = "ADAPTER-USB", ProductName = "USB-C Hub Adapter", Quantity = 1, UnitPrice = 99.99m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "321 Elm Drive",
                    City = "Austin",
                    State = "TX",
                    PostalCode = "73301",
                    Country = "USA"
                },
                Priority = "EXPRESS",
                Status = OrderStatus.Processing
            },
            new Order
            {
                OrderId = "ORD-2026-005",
                CustomerName = "Charlie Brown",
                CustomerEmail = "charlie.brown@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-12),
                TotalAmount = 799.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "WATCH-APPLE-S9", ProductName = "Apple Watch Series 9", Quantity = 1, UnitPrice = 699.99m },
                    new() { Sku = "BAND-SPORT", ProductName = "Sport Band", Quantity = 1, UnitPrice = 100.00m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "555 Maple Lane",
                    City = "Denver",
                    State = "CO",
                    PostalCode = "80202",
                    Country = "USA"
                },
                Priority = "OVERNIGHT",
                Status = OrderStatus.Pending
            },
            new Order
            {
                OrderId = "ORD-2026-006",
                CustomerName = "Diana Martinez",
                CustomerEmail = "diana.martinez@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-8),
                TotalAmount = 3499.98m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "MACBOOK-PRO-16", ProductName = "MacBook Pro 16\"", Quantity = 1, UnitPrice = 2999.99m },
                    new() { Sku = "MOUSE-MX-MASTER", ProductName = "Logitech MX Master 3S", Quantity = 1, UnitPrice = 99.99m },
                    new() { Sku = "SLEEVE-LAPTOP", ProductName = "Premium Laptop Sleeve", Quantity = 1, UnitPrice = 400.00m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "888 Broadway",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10003",
                    Country = "USA"
                },
                Priority = "EXPRESS",
                Status = OrderStatus.Pending
            },
            new Order
            {
                OrderId = "ORD-2026-007",
                CustomerName = "Ethan Davis",
                CustomerEmail = "ethan.davis@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-6),
                TotalAmount = 549.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "SPEAKER-SONOS", ProductName = "Sonos One Smart Speaker", Quantity = 2, UnitPrice = 219.99m },
                    new() { Sku = "STAND-SPEAKER", ProductName = "Speaker Stand Pair", Quantity = 1, UnitPrice = 110.01m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "777 Beach Boulevard",
                    City = "Miami",
                    State = "FL",
                    PostalCode = "33139",
                    Country = "USA"
                },
                Priority = "STANDARD",
                Status = OrderStatus.Pending
            },
            new Order
            {
                OrderId = "ORD-2026-008",
                CustomerName = "Fiona Garcia",
                CustomerEmail = "fiona.garcia@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-4),
                TotalAmount = 1899.97m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "MONITOR-LG-34", ProductName = "LG 34\" Ultrawide Monitor", Quantity = 1, UnitPrice = 899.99m },
                    new() { Sku = "DESK-STAND", ProductName = "Adjustable Monitor Arm", Quantity = 1, UnitPrice = 149.99m },
                    new() { Sku = "CABLE-DP", ProductName = "DisplayPort Cable 2m", Quantity = 1, UnitPrice = 29.99m },
                    new() { Sku = "WEBCAM-LOGITECH", ProductName = "Logitech Brio 4K Webcam", Quantity = 1, UnitPrice = 199.99m },
                    new() { Sku = "LIGHT-RING", ProductName = "USB Ring Light", Quantity = 1, UnitPrice = 69.99m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "999 Silicon Valley Road",
                    City = "San Jose",
                    State = "CA",
                    PostalCode = "95110",
                    Country = "USA"
                },
                Priority = "EXPRESS",
                Status = OrderStatus.Pending
            },
            new Order
            {
                OrderId = "ORD-2026-009",
                CustomerName = "George Wilson",
                CustomerEmail = "george.wilson@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-2),
                TotalAmount = 299.99m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "KEYBOARD-MECH", ProductName = "Mechanical Gaming Keyboard", Quantity = 1, UnitPrice = 179.99m },
                    new() { Sku = "MOUSEPAD-XL", ProductName = "Extended Gaming Mouse Pad", Quantity = 1, UnitPrice = 39.99m },
                    new() { Sku = "WRIST-REST", ProductName = "Keyboard Wrist Rest", Quantity = 1, UnitPrice = 80.01m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "147 Tech Park",
                    City = "Boston",
                    State = "MA",
                    PostalCode = "02108",
                    Country = "USA"
                },
                Priority = "STANDARD",
                Status = OrderStatus.Pending
            },
            new Order
            {
                OrderId = "ORD-2026-010",
                CustomerName = "Hannah Lee",
                CustomerEmail = "hannah.lee@example.com",
                OrderDate = DateTime.UtcNow.AddHours(-1),
                TotalAmount = 4999.95m,
                Currency = "USD",
                Items = new List<OrderItem>
                {
                    new() { Sku = "TV-OLED-65", ProductName = "LG 65\" OLED TV", Quantity = 1, UnitPrice = 2499.99m },
                    new() { Sku = "SOUNDBAR-SONOS", ProductName = "Sonos Arc Soundbar", Quantity = 1, UnitPrice = 899.99m },
                    new() { Sku = "MOUNT-TV-WALL", ProductName = "Full Motion TV Wall Mount", Quantity = 1, UnitPrice = 149.99m },
                    new() { Sku = "HDMI-CABLE-8K", ProductName = "Premium 8K HDMI Cable", Quantity = 2, UnitPrice = 79.99m },
                    new() { Sku = "STREAMING-APPLE-TV", ProductName = "Apple TV 4K", Quantity = 1, UnitPrice = 179.99m }
                },
                ShippingAddress = new ShippingAddress
                {
                    Street = "258 Lake Shore Drive",
                    City = "Chicago",
                    State = "IL",
                    PostalCode = "60601",
                    Country = "USA"
                },
                Priority = "EXPRESS",
                Status = OrderStatus.Pending
            }
        };

        foreach (var order in seedOrders)
        {
            _orders.TryAdd(order.OrderId, order);
        }

        _logger.LogInformation("Seeded {Count} sample orders", seedOrders.Length);
    }

    private static string GenerateOrderId()
    {
        var timestamp = DateTime.UtcNow;
        var random = Random.Shared.Next(1000, 9999);
        return $"ORD-{timestamp:yyyy}-{random}";
    }

    /// <summary>
    /// Removes CR/LF from user-supplied strings before logging to prevent log-forging.
    /// </summary>
    private static string SanitizeForLog(string? value) =>
        value?.Replace("\r", "\\r", StringComparison.Ordinal)
              .Replace("\n", "\\n", StringComparison.Ordinal) ?? string.Empty;
}
