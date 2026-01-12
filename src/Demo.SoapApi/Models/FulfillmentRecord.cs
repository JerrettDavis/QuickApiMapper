namespace Demo.SoapApi.Models;

/// <summary>
/// Internal storage model for fulfillment requests
/// </summary>
public class FulfillmentRecord
{
    public string ConfirmationNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedDateTime { get; set; }
    public DateTime? ProcessedDateTime { get; set; }
    public DateTime? ShippedDateTime { get; set; }
    public DateTime? DeliveredDateTime { get; set; }
    public DateTime? CancelledDateTime { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public decimal TotalValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string PriorityCode { get; set; } = "STD";
    public int ItemCount { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
}
