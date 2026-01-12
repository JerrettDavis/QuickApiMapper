using Demo.JsonApi.Models;

namespace Demo.JsonApi.Services;

/// <summary>
/// Service interface for managing orders
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Create a new order
    /// </summary>
    Task<Order> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all orders
    /// </summary>
    Task<IEnumerable<Order>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific order by ID
    /// </summary>
    Task<Order?> GetOrderByIdAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update order status
    /// </summary>
    Task<Order?> UpdateOrderStatusAsync(string orderId, OrderStatus status, CancellationToken cancellationToken = default);
}
