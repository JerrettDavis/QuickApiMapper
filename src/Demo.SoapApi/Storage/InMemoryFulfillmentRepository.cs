using System.Collections.Concurrent;
using Demo.SoapApi.Models;

namespace Demo.SoapApi.Storage;

/// <summary>
/// In-memory implementation of fulfillment repository for demo purposes
/// In a real system, this would be backed by a database
/// </summary>
public class InMemoryFulfillmentRepository : IFulfillmentRepository
{
    private readonly ConcurrentDictionary<string, FulfillmentRecord> _fulfillmentsByConfirmation = new();
    private readonly ConcurrentDictionary<string, string> _orderToConfirmationMap = new();
    private readonly ILogger<InMemoryFulfillmentRepository> _logger;

    public InMemoryFulfillmentRepository(ILogger<InMemoryFulfillmentRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(FulfillmentRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ConfirmationNumber))
        {
            throw new ArgumentException("Confirmation number is required", nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.OrderNumber))
        {
            throw new ArgumentException("Order number is required", nameof(record));
        }

        if (!_fulfillmentsByConfirmation.TryAdd(record.ConfirmationNumber, record))
        {
            throw new InvalidOperationException($"Fulfillment with confirmation number {record.ConfirmationNumber} already exists");
        }

        _orderToConfirmationMap[record.OrderNumber] = record.ConfirmationNumber;

        _logger.LogDebug("Added fulfillment {ConfirmationNumber} for order {OrderNumber}",
            record.ConfirmationNumber, record.OrderNumber);

        return Task.CompletedTask;
    }

    public Task UpdateAsync(FulfillmentRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ConfirmationNumber))
        {
            throw new ArgumentException("Confirmation number is required", nameof(record));
        }

        if (!_fulfillmentsByConfirmation.ContainsKey(record.ConfirmationNumber))
        {
            throw new InvalidOperationException($"Fulfillment with confirmation number {record.ConfirmationNumber} not found");
        }

        _fulfillmentsByConfirmation[record.ConfirmationNumber] = record;

        _logger.LogDebug("Updated fulfillment {ConfirmationNumber}", record.ConfirmationNumber);

        return Task.CompletedTask;
    }

    public Task<FulfillmentRecord?> GetByConfirmationNumberAsync(string confirmationNumber)
    {
        _fulfillmentsByConfirmation.TryGetValue(confirmationNumber, out var record);
        return Task.FromResult(record);
    }

    public Task<FulfillmentRecord?> GetByOrderNumberAsync(string orderNumber)
    {
        if (_orderToConfirmationMap.TryGetValue(orderNumber, out var confirmationNumber))
        {
            _fulfillmentsByConfirmation.TryGetValue(confirmationNumber, out var record);
            return Task.FromResult(record);
        }

        return Task.FromResult<FulfillmentRecord?>(null);
    }

    public Task<IEnumerable<FulfillmentRecord>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<FulfillmentRecord>>(_fulfillmentsByConfirmation.Values.ToList());
    }
}
