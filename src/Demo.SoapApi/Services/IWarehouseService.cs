using System.ServiceModel;
using Demo.SoapApi.Models;

namespace Demo.SoapApi.Services;

/// <summary>
/// Legacy warehouse/ERP SOAP service interface
/// </summary>
[ServiceContract(Namespace = "http://warehouse.example.com/")]
public interface IWarehouseService
{
    [OperationContract]
    Task<SubmitFulfillmentResponse> SubmitFulfillmentRequest(SubmitFulfillmentRequest request);

    [OperationContract]
    Task<GetFulfillmentStatusResponse> GetFulfillmentStatus(GetFulfillmentStatusRequest request);

    [OperationContract]
    Task<CancelFulfillmentResponse> CancelFulfillment(CancelFulfillmentRequest request);
}
