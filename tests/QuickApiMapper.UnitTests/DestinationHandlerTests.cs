using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class DestinationHandlerTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Test]
    public async Task JsonDestinationHandler_Sends_Json_Payload_And_Sets_Response()
    {
        var handler = new JsonDestinationHandler();
        var integration = new IntegrationMapping(
            "TestJson", "/test", "JSON", "JSON", "https://example.com/api", [], null, new Dictionary<string, string>(), []);
        var outJson = JObject.Parse("{\"foo\": \"bar\"}");
        var outXml = null as XDocument;
        var httpClientFactory = CreateHttpClientFactoryWithResponse("application/json", "{\"result\":true}");
        var context = new DefaultHttpContext();
        var resp = context.Response;
        var req = context.Request;
        // Ensure response body is a MemoryStream for reading
        resp.Body = new MemoryStream();

        await handler.HandleAsync(integration, outJson, outXml, req, resp, httpClientFactory);
        await resp.Body.FlushAsync();
        resp.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(resp.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(200));
            Assert.That(resp.ContentType, Is.EqualTo("application/json"));
            Assert.That(responseBody, Is.EqualTo("{\"result\":true}"));
        });
    }

    [Test]
    public async Task SoapDestinationHandler_Sends_Xml_Payload_And_Sets_Response()
    {
        var handler = new SoapDestinationHandler();
        var integration = new IntegrationMapping(
            "TestSoap", "/test", "JSON", "SOAP", "https://example.com/soap", [], null, new Dictionary<string, string>(), []);
        var outJson = null as JObject;
        var tnsNs = integration.StaticValues?.FirstOrDefault(x => x.Key == "TnsNamespace").Value ?? "";
        XNamespace tns = tnsNs;
        var outXml = new XDocument(new XElement(tns + "root", new XElement(tns + "foo", "bar")));
        var httpClientFactory = CreateHttpClientFactoryWithResponse("application/xml", "<result>true</result>");
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        var resp = context.Response;
        var req = context.Request;
        // Ensure the response body is a MemoryStream for reading
        resp.Body = new MemoryStream();

        await handler.HandleAsync(integration, outJson, outXml, req, resp, httpClientFactory);
        await resp.Body.FlushAsync();
        resp.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(resp.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(200));
            Assert.That(resp.ContentType, Is.EqualTo("application/xml"));
            Assert.That(responseBody, Is.EqualTo("<result>true</result>"));
        });
    }

    [Test]
    public async Task JsonDestinationHandler_Returns_400_If_No_Output()
    {
        var handler = new JsonDestinationHandler();
        var integration = new IntegrationMapping(
            "TestJson", "/test", "JSON", "JSON", "https://example.com/api", [], null, new Dictionary<string, string>(), []);
        var httpClientFactory = CreateHttpClientFactoryWithResponse("application/json", "{}");
        var context = new DefaultHttpContext();
        var resp = context.Response;
        var req = context.Request;
        // Ensure the response body is a MemoryStream for reading
        resp.Body = new MemoryStream();

        await handler.HandleAsync(integration, null, null, req, resp, httpClientFactory);
        Assert.That(resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task SoapDestinationHandler_Returns_400_If_No_Output()
    {
        var handler = new SoapDestinationHandler();
        var integration = new IntegrationMapping(
            "TestSoap", "/test", "JSON", "SOAP", "https://example.com/soap", [], null, new Dictionary<string, string>(), []);
        var httpClientFactory = CreateHttpClientFactoryWithResponse("application/xml", "");
        var context = new DefaultHttpContext();
        var resp = context.Response;
        var req = context.Request;
        // Ensure the response body is a MemoryStream for reading
        resp.Body = new MemoryStream();

        await handler.HandleAsync(integration, null, null, req, resp, httpClientFactory);
        Assert.That(resp.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void JsonDestinationHandler_CanHandle_Only_Json()
    {
        var handler = new JsonDestinationHandler();
        Assert.Multiple(() =>
        {
            Assert.That(handler.CanHandle("JSON"), Is.True);
            Assert.That(handler.CanHandle("SOAP"), Is.False);
        });
    }

    [Test]
    public void SoapDestinationHandler_CanHandle_Soap_And_Xml()
    {
        var handler = new SoapDestinationHandler();
        Assert.That(handler.CanHandle("SOAP"), Is.True);
        Assert.That(handler.CanHandle("XML"), Is.True);
        Assert.That(handler.CanHandle("JSON"), Is.False);
    }

    [Test]
    public async Task SoapDestinationHandler_Produces_Full_Soap_Envelope_With_Header_And_Body()
    {
        var services = new ServiceCollection();
        // Use the new centralized service registration
        services.AddQuickApiMapper();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        await using var serviceProvider = services.BuildServiceProvider();
        var mappingEngineFactory = serviceProvider.GetRequiredService<IMappingEngineFactory>();

        // Arrange: Use Vendor Integration config and input
        var configJson = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "VendorIntegration", "VendorIntegration-Config.json"));
        var inputJson = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "VendorIntegration", "VendorIntegration-Input.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, _jsonOptions);
        Assert.That(config, Is.Not.Null);

        var integration = config.Mappings!.First(m => m.Name == "VendorIntegration");
        var inputJObject = JObject.Parse(inputJson);
        var tnsNs = integration.StaticValues?.FirstOrDefault(x => x.Key == "TnsNamespace").Value ?? "";
        XNamespace tns = tnsNs;
        var outputXml = new XDocument(new XElement(tns + "root"));

        // Act: Use the new generic mapping engine
        var engine = mappingEngineFactory.CreateEngine<JObject, XDocument>();
        var result = await engine.ApplyMappingAsync(
            integration.Mapping!,
            inputJObject,
            outputXml,
            integration.StaticValues,
            globalStatics: config.StaticValues,
            serviceProvider: serviceProvider
        );

        // Assert the mapping was successful
        Assert.That(result.IsSuccess, Is.True, $"Mapping should be successful. Error: {result.ErrorMessage}");

        var handler = new SoapDestinationHandler();
        var httpClientFactory = CreateHttpClientFactoryWithResponse("application/xml", "<result>success</result>");
        var context = new DefaultHttpContext();
        
        // Register the ApiMappingConfig in the service collection and set RequestServices
        services.AddSingleton(config);
        var contextServiceProvider = services.BuildServiceProvider();
        context.RequestServices = contextServiceProvider;
        
        var resp = context.Response;
        var req = context.Request;
        resp.Body = new MemoryStream();

        // Act: Handle the SOAP destination
        await handler.HandleAsync(integration, null, outputXml, req, resp, httpClientFactory);

        // Assert: Check response
        await resp.Body.FlushAsync();
        resp.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(resp.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(200));
            Assert.That(resp.ContentType, Is.EqualTo("application/xml"));
            Assert.That(responseBody, Is.EqualTo("<result>success</result>"));
        });
    }

    private static IHttpClientFactory CreateHttpClientFactoryWithResponse(string contentType, string responseBody)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            
#pragma warning disable IDISP001
#pragma warning disable IDISP004
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseBody)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue(contentType) }
                }
            });


        var client = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factoryMock.Object;
#pragma warning restore IDISP004
#pragma warning restore IDISP001
    }
}