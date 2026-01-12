using Demo.SoapApi.Models;
using Demo.SoapApi.Storage;

namespace Demo.SoapApi.Services;

/// <summary>
/// Implementation of the legacy warehouse/ERP SOAP service
/// Simulates a typical enterprise system that QuickApiMapper will integrate with
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly IFulfillmentRepository _repository;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(IFulfillmentRepository repository, ILogger<WarehouseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SubmitFulfillmentResponse> SubmitFulfillmentRequest(SubmitFulfillmentRequest request)
    {
        _logger.LogInformation("Received fulfillment request for order {OrderNumber}", request.OrderNumber);

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                return new SubmitFulfillmentResponse
                {
                    Success = false,
                    OrderNumber = request.OrderNumber,
                    ProcessedDateTime = DateTime.UtcNow,
                    Status = "REJECTED",
                    Message = "Order number is required"
                };
            }

            if (request.LineItems == null || request.LineItems.Count == 0)
            {
                return new SubmitFulfillmentResponse
                {
                    Success = false,
                    OrderNumber = request.OrderNumber,
                    ProcessedDateTime = DateTime.UtcNow,
                    Status = "REJECTED",
                    Message = "At least one line item is required"
                };
            }

            // Generate confirmation number
            var confirmationNumber = $"WH-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // Calculate estimated ship date based on priority
            var estimatedShipDate = request.PriorityCode?.ToUpper() switch
            {
                "EXP" or "EXPRESS" => DateTime.UtcNow.AddDays(1),
                "PRI" or "PRIORITY" => DateTime.UtcNow.AddDays(2),
                _ => DateTime.UtcNow.AddDays(3) // Standard
            };

            // Create fulfillment record
            var record = new FulfillmentRecord
            {
                ConfirmationNumber = confirmationNumber,
                OrderNumber = request.OrderNumber,
                Status = "PENDING",
                SubmittedDateTime = DateTime.UtcNow,
                ProcessedDateTime = DateTime.UtcNow,
                TotalValue = request.TotalValue,
                CurrencyCode = request.CurrencyCode,
                CustomerName = request.CustomerInfo?.Name ?? string.Empty,
                CustomerEmail = request.CustomerInfo?.ContactEmail ?? string.Empty,
                PriorityCode = request.PriorityCode ?? "STD",
                ItemCount = request.LineItems.Count
            };

            // Store the fulfillment
            await _repository.AddAsync(record);

            _logger.LogInformation("Fulfillment request accepted. Confirmation: {ConfirmationNumber}", confirmationNumber);

            return new SubmitFulfillmentResponse
            {
                Success = true,
                ConfirmationNumber = confirmationNumber,
                OrderNumber = request.OrderNumber,
                ProcessedDateTime = DateTime.UtcNow,
                Status = "PENDING",
                Message = "Fulfillment request accepted and queued for processing",
                EstimatedShipDate = estimatedShipDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fulfillment request for order {OrderNumber}", request.OrderNumber);
            return new SubmitFulfillmentResponse
            {
                Success = false,
                OrderNumber = request.OrderNumber,
                ProcessedDateTime = DateTime.UtcNow,
                Status = "ERROR",
                Message = $"Internal error: {ex.Message}"
            };
        }
    }

    public async Task<GetFulfillmentStatusResponse> GetFulfillmentStatus(GetFulfillmentStatusRequest request)
    {
        _logger.LogInformation("Status query for Order: {OrderNumber}, Confirmation: {ConfirmationNumber}",
            request.OrderNumber, request.ConfirmationNumber);

        try
        {
            FulfillmentRecord? record = null;

            if (!string.IsNullOrWhiteSpace(request.ConfirmationNumber))
            {
                record = await _repository.GetByConfirmationNumberAsync(request.ConfirmationNumber);
            }
            else if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                record = await _repository.GetByOrderNumberAsync(request.OrderNumber);
            }

            if (record == null)
            {
                return new GetFulfillmentStatusResponse
                {
                    Success = false,
                    ErrorMessage = "Fulfillment not found"
                };
            }

            // Simulate status progression for demo purposes
            // In a real system, this would be updated by warehouse operations
            SimulateStatusProgression(record);

            return new GetFulfillmentStatusResponse
            {
                Success = true,
                FulfillmentStatus = new FulfillmentStatusInfo
                {
                    OrderNumber = record.OrderNumber,
                    ConfirmationNumber = record.ConfirmationNumber,
                    Status = record.Status,
                    SubmittedDateTime = record.SubmittedDateTime,
                    ProcessedDateTime = record.ProcessedDateTime,
                    ShippedDateTime = record.ShippedDateTime,
                    DeliveredDateTime = record.DeliveredDateTime,
                    TrackingNumber = record.TrackingNumber,
                    Carrier = record.Carrier,
                    TotalValue = record.TotalValue,
                    CurrencyCode = record.CurrencyCode
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fulfillment status");
            return new GetFulfillmentStatusResponse
            {
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}"
            };
        }
    }

    public async Task<CancelFulfillmentResponse> CancelFulfillment(CancelFulfillmentRequest request)
    {
        _logger.LogInformation("Cancellation request for Order: {OrderNumber}, Confirmation: {ConfirmationNumber}",
            request.OrderNumber, request.ConfirmationNumber);

        try
        {
            FulfillmentRecord? record = null;

            if (!string.IsNullOrWhiteSpace(request.ConfirmationNumber))
            {
                record = await _repository.GetByConfirmationNumberAsync(request.ConfirmationNumber);
            }
            else if (!string.IsNullOrWhiteSpace(request.OrderNumber))
            {
                record = await _repository.GetByOrderNumberAsync(request.OrderNumber);
            }

            if (record == null)
            {
                return new CancelFulfillmentResponse
                {
                    Success = false,
                    ErrorMessage = "Fulfillment not found"
                };
            }

            // Check if cancellation is allowed
            if (record.Status == "CANCELLED")
            {
                return new CancelFulfillmentResponse
                {
                    Success = false,
                    OrderNumber = record.OrderNumber,
                    ConfirmationNumber = record.ConfirmationNumber,
                    ErrorMessage = "Fulfillment already cancelled"
                };
            }

            if (record.Status == "DELIVERED")
            {
                return new CancelFulfillmentResponse
                {
                    Success = false,
                    OrderNumber = record.OrderNumber,
                    ConfirmationNumber = record.ConfirmationNumber,
                    ErrorMessage = "Cannot cancel - order already delivered"
                };
            }

            // Update record
            record.Status = "CANCELLED";
            record.CancelledDateTime = DateTime.UtcNow;
            record.CancellationReason = request.Reason;
            record.CancelledBy = request.RequestedBy;

            await _repository.UpdateAsync(record);

            _logger.LogInformation("Fulfillment {ConfirmationNumber} cancelled successfully", record.ConfirmationNumber);

            return new CancelFulfillmentResponse
            {
                Success = true,
                OrderNumber = record.OrderNumber,
                ConfirmationNumber = record.ConfirmationNumber,
                CancelledDateTime = record.CancelledDateTime.Value,
                Message = "Fulfillment cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling fulfillment");
            return new CancelFulfillmentResponse
            {
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Simulate status progression for demo purposes
    /// In a real system, this would be updated by actual warehouse operations
    /// </summary>
    private void SimulateStatusProgression(FulfillmentRecord record)
    {
        if (record.Status == "CANCELLED")
        {
            return; // Don't progress cancelled orders
        }

        var hoursSinceSubmission = (DateTime.UtcNow - record.SubmittedDateTime).TotalHours;

        // Simulate processing based on priority
        var processingHours = record.PriorityCode?.ToUpper() switch
        {
            "EXP" or "EXPRESS" => 2,
            "PRI" or "PRIORITY" => 4,
            _ => 8 // Standard
        };

        if (hoursSinceSubmission >= processingHours && record.Status == "PENDING")
        {
            record.Status = "PROCESSING";
        }

        if (hoursSinceSubmission >= processingHours + 4 && record.Status == "PROCESSING")
        {
            record.Status = "SHIPPED";
            record.ShippedDateTime = record.SubmittedDateTime.AddHours(processingHours + 4);
            record.TrackingNumber = $"TRK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..10].ToUpper()}";
            record.Carrier = Random.Shared.Next(3) switch
            {
                0 => "FedEx",
                1 => "UPS",
                _ => "USPS"
            };
        }

        // Simulate delivery after 2-5 days depending on priority
        var deliveryDays = record.PriorityCode?.ToUpper() switch
        {
            "EXP" or "EXPRESS" => 1,
            "PRI" or "PRIORITY" => 2,
            _ => 3 // Standard
        };

        if (hoursSinceSubmission >= (processingHours + 4 + (deliveryDays * 24)) && record.Status == "SHIPPED")
        {
            record.Status = "DELIVERED";
            record.DeliveredDateTime = record.ShippedDateTime?.AddDays(deliveryDays);
        }
    }
}
