namespace Demo.JsonApi.Models;

/// <summary>
/// Represents an e-commerce order
/// </summary>
public class Order
{
    /// <summary>
    /// Unique order identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Customer's full name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the order was placed
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Total order amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR, GBP)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// List of items in the order
    /// </summary>
    public List<OrderItem> Items { get; set; } = new();

    /// <summary>
    /// Shipping address for the order
    /// </summary>
    public ShippingAddress ShippingAddress { get; set; } = new();

    /// <summary>
    /// Order priority (STANDARD, EXPRESS, OVERNIGHT)
    /// </summary>
    public string Priority { get; set; } = "STANDARD";

    /// <summary>
    /// Current order status
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

/// <summary>
/// Represents an item in an order
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Stock Keeping Unit identifier
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Represents a shipping address
/// </summary>
public class ShippingAddress
{
    /// <summary>
    /// Street address
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
