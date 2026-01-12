using System.Runtime.Serialization;

namespace Demo.SoapApi.Models;

[DataContract(Namespace = "http://warehouse.example.com/")]
public class CustomerInfo
{
    [DataMember(Order = 1)]
    public string Name { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string ContactEmail { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class LineItem
{
    [DataMember(Order = 1)]
    public string SKU { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Description { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public int Qty { get; set; }

    [DataMember(Order = 4)]
    public decimal Price { get; set; }
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class DeliveryAddress
{
    [DataMember(Order = 1)]
    public string AddressLine1 { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string? AddressLine2 { get; set; }

    [DataMember(Order = 3)]
    public string City { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string StateProvince { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string PostalCode { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public string CountryCode { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class SubmitFulfillmentRequest
{
    [DataMember(Order = 1)]
    public string OrderNumber { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public CustomerInfo CustomerInfo { get; set; } = new();

    [DataMember(Order = 3)]
    public DateTime OrderDateTime { get; set; }

    [DataMember(Order = 4)]
    public decimal TotalValue { get; set; }

    [DataMember(Order = 5)]
    public string CurrencyCode { get; set; } = "USD";

    [DataMember(Order = 6)]
    public List<LineItem> LineItems { get; set; } = new();

    [DataMember(Order = 7)]
    public DeliveryAddress DeliveryAddress { get; set; } = new();

    [DataMember(Order = 8)]
    public string PriorityCode { get; set; } = "STD";
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class SubmitFulfillmentResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string ConfirmationNumber { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string OrderNumber { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public DateTime ProcessedDateTime { get; set; }

    [DataMember(Order = 5)]
    public string Status { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public string? Message { get; set; }

    [DataMember(Order = 7)]
    public DateTime? EstimatedShipDate { get; set; }
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class GetFulfillmentStatusRequest
{
    [DataMember(Order = 1)]
    public string? OrderNumber { get; set; }

    [DataMember(Order = 2)]
    public string? ConfirmationNumber { get; set; }
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class FulfillmentStatusInfo
{
    [DataMember(Order = 1)]
    public string OrderNumber { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string ConfirmationNumber { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Status { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public DateTime SubmittedDateTime { get; set; }

    [DataMember(Order = 5)]
    public DateTime? ProcessedDateTime { get; set; }

    [DataMember(Order = 6)]
    public DateTime? ShippedDateTime { get; set; }

    [DataMember(Order = 7)]
    public DateTime? DeliveredDateTime { get; set; }

    [DataMember(Order = 8)]
    public string? TrackingNumber { get; set; }

    [DataMember(Order = 9)]
    public string? Carrier { get; set; }

    [DataMember(Order = 10)]
    public decimal TotalValue { get; set; }

    [DataMember(Order = 11)]
    public string CurrencyCode { get; set; } = "USD";
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class GetFulfillmentStatusResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public FulfillmentStatusInfo? FulfillmentStatus { get; set; }

    [DataMember(Order = 3)]
    public string? ErrorMessage { get; set; }
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class CancelFulfillmentRequest
{
    [DataMember(Order = 1)]
    public string? OrderNumber { get; set; }

    [DataMember(Order = 2)]
    public string? ConfirmationNumber { get; set; }

    [DataMember(Order = 3)]
    public string Reason { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string RequestedBy { get; set; } = string.Empty;
}

[DataContract(Namespace = "http://warehouse.example.com/")]
public class CancelFulfillmentResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string OrderNumber { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string ConfirmationNumber { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public DateTime CancelledDateTime { get; set; }

    [DataMember(Order = 5)]
    public string? Message { get; set; }

    [DataMember(Order = 6)]
    public string? ErrorMessage { get; set; }
}
