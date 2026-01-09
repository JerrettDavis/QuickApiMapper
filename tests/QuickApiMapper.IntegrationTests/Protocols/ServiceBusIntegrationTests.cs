using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;
using QuickApiMapper.Extensions.ServiceBus.Destinations;

namespace QuickApiMapper.IntegrationTests.Protocols;

/// <summary>
/// Integration tests for Azure Service Bus protocol extension.
/// Uses mocking for Azure Service Bus client since real instances require Azure credentials.
/// For full end-to-end testing, run against a development Service Bus namespace.
/// </summary>
[TestFixture]
public class ServiceBusIntegrationTests
{
    private Mock<ServiceBusClient>? _mockServiceBusClient;
    private Mock<ServiceBusSender>? _mockSender;
    private ILogger<ServiceBusDestinationHandler>? _logger;
    private ILoggerFactory? _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockSender = new Mock<ServiceBusSender>();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = _loggerFactory.CreateLogger<ServiceBusDestinationHandler>();

        _mockServiceBusClient
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns(_mockSender.Object);
    }

    private static IntegrationMapping CreateIntegrationMapping(
        string name,
        string endpoint,
        string destinationUrl,
        string destinationType,
        IReadOnlyDictionary<string, string>? staticValues = null)
    {
        return new IntegrationMapping(
            Name: name,
            Endpoint: endpoint,
            SourceType: "JSON",
            DestinationType: destinationType,
            DestinationUrl: destinationUrl,
            PayloadArguments: null,
            DispatchFor: null,
            StaticValues: staticValues,
            Mapping: null,
            SoapHeaderXml: null,
            SoapConfig: null);
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldSendJsonMessageToQueue()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-queue",
            destinationType: "ServiceBus");

        var outputJson = JObject.Parse(@"{
            ""orderId"": 12345,
            ""customerName"": ""John Doe"",
            ""amount"": 99.99
        }");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        ServiceBusMessage? capturedMessage = null;
        _mockSender!
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(
            integration,
            outputJson,
            null,
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        _mockSender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedMessage.Should().NotBeNull();
        capturedMessage!.ContentType.Should().Be("application/json");
        capturedMessage.MessageId.Should().NotBeNullOrEmpty();

        var messageBody = capturedMessage.Body.ToString();
        var receivedJson = JObject.Parse(messageBody);
        receivedJson["orderId"]!.Value<int>().Should().Be(12345);
        receivedJson["customerName"]!.Value<string>().Should().Be("John Doe");
        receivedJson["amount"]!.Value<decimal>().Should().Be(99.99m);
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldSendXmlMessageToQueue()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-queue",
            destinationType: "ServiceBus");

        var outputXml = System.Xml.Linq.XDocument.Parse(@"
            <Order>
                <OrderId>67890</OrderId>
                <Customer>Jane Smith</Customer>
                <Total>249.50</Total>
            </Order>");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        ServiceBusMessage? capturedMessage = null;
        _mockSender!
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(
            integration,
            null,
            outputXml,
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        _mockSender.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedMessage.Should().NotBeNull();
        capturedMessage!.ContentType.Should().Be("application/xml");

        var messageBody = capturedMessage.Body.ToString();
        messageBody.Should().Contain("<OrderId>67890</OrderId>");
        messageBody.Should().Contain("<Customer>Jane Smith</Customer>");
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldIncludeStaticValuesAsApplicationProperties()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-queue",
            destinationType: "ServiceBus",
            staticValues: new Dictionary<string, string>
            {
                { "Source-System", "QuickApiMapper" },
                { "Version", "2.0" },
                { "Environment", "Production" }
            });

        var outputJson = JObject.Parse(@"{""test"": ""data""}");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        ServiceBusMessage? capturedMessage = null;
        _mockSender!
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(
            integration,
            outputJson,
            null,
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.ApplicationProperties.Should().ContainKey("Source-System");
        capturedMessage.ApplicationProperties.Should().ContainKey("Version");
        capturedMessage.ApplicationProperties.Should().ContainKey("Environment");

        capturedMessage.ApplicationProperties["Source-System"].Should().Be("QuickApiMapper");
        capturedMessage.ApplicationProperties["Version"].Should().Be("2.0");
        capturedMessage.ApplicationProperties["Environment"].Should().Be("Production");
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldHandleTopicUrl()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-topic",
            destinationType: "ServiceBus");

        var outputJson = JObject.Parse(@"{""event"": ""user.created""}");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        // Act
        await handler.HandleAsync(
            integration,
            outputJson,
            null,
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        _mockServiceBusClient!.Verify(
            x => x.CreateSender("test-topic"),
            Times.Once);

        _mockSender!.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldReturnErrorWhenNoOutputData()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-queue",
            destinationType: "ServiceBus");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        // Act
        await handler.HandleAsync(
            integration,
            null, // No JSON
            null, // No XML
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        response.Body.Position = 0;
        string responseBody;
        using (var reader = new StreamReader(response.Body))
        {
            responseBody = await reader.ReadToEndAsync();
        }
        responseBody.Should().Contain("No output data available");

        _mockSender!.Verify(
            x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ServiceBusDestinationHandler_ShouldSetMessageIdAndSubject()
    {
        // Arrange
        var handler = new ServiceBusDestinationHandler(_logger!, _mockServiceBusClient!.Object);

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            endpoint: "/test",
            destinationUrl: "servicebus://test-queue",
            destinationType: "ServiceBus");

        var outputJson = JObject.Parse(@"{""data"": ""test""}");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

        ServiceBusMessage? capturedMessage = null;
        _mockSender!
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, ct) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(
            integration,
            outputJson,
            null,
            request,
            response,
            null!,
            CancellationToken.None);

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.MessageId.Should().NotBeNullOrEmpty();
        capturedMessage.Subject.Should().Be("QuickApiMapper.TestIntegration");

        // Verify MessageId is a valid GUID
        Guid.TryParse(capturedMessage.MessageId, out var messageIdGuid).Should().BeTrue();
        messageIdGuid.Should().NotBe(Guid.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
        _mockSender = null;
        _mockServiceBusClient = null;
    }
}
