namespace Demo.JsonApi.Models;

/// <summary>
/// Request model for updating order status
/// </summary>
public class OrderStatusUpdate
{
    /// <summary>
    /// New status for the order
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Optional notes about the status change
    /// </summary>
    public string? Notes { get; set; }
}
