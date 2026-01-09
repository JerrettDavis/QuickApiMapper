using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Destinations;

public interface IDestinationHandler
{
    bool CanHandle(string destinationType);

    Task HandleAsync(
        IntegrationMapping integration,
        JObject? outJson,
        XDocument? outXml,
        HttpRequest req,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default);
}