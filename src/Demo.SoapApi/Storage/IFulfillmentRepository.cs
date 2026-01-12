using Demo.SoapApi.Models;

namespace Demo.SoapApi.Storage;

/// <summary>
/// Repository interface for fulfillment data storage
/// </summary>
public interface IFulfillmentRepository
{
    Task AddAsync(FulfillmentRecord record);
    Task UpdateAsync(FulfillmentRecord record);
    Task<FulfillmentRecord?> GetByConfirmationNumberAsync(string confirmationNumber);
    Task<FulfillmentRecord?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<FulfillmentRecord>> GetAllAsync();
}
