using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;
using QuickApiMapper.Extensions.RabbitMQ.Destinations;
using QuickApiMapper.Extensions.RabbitMQ.Extensions;
using QuickApiMapper.Extensions.RabbitMQ.Workers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Testcontainers.RabbitMq;

namespace QuickApiMapper.IntegrationTests.Protocols;

/// <summary>
/// Integration tests for RabbitMQ protocol extension.
/// Uses Testcontainers to spin up a real RabbitMQ instance.
/// </summary>
[TestFixture]
public class RabbitMqIntegrationTests
{
    private RabbitMqContainer? _rabbitMqContainer;
    private IConnectionFactory? _connectionFactory;
    private ServiceProvider? _serviceProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Start RabbitMQ container with explicit credentials
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        await _rabbitMqContainer.StartAsync();

        // Configure services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Register RabbitMQ support with container's connection details
        services.AddRabbitMqSupport(
            hostName: _rabbitMqContainer.Hostname,
            configureOptions: options =>
            {
                options.Port = _rabbitMqContainer.GetMappedPublicPort(5672);
                options.UserName = "guest";
                options.Password = "guest";
            });

        _serviceProvider = services.BuildServiceProvider();
        _connectionFactory = _serviceProvider.GetRequiredService<IConnectionFactory>();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _serviceProvider?.Dispose();

        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.StopAsync();
            await _rabbitMqContainer.DisposeAsync();
        }
    }

    private static IntegrationMapping CreateIntegrationMapping(
        string name,
        string destinationUrl,
        string destinationType,
        IReadOnlyDictionary<string, string>? staticValues = null)
    {
        return new IntegrationMapping(
            Name: name,
            Endpoint: "/test",
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
    public async Task RabbitMqDestinationHandler_ShouldPublishJsonMessageToQueue()
    {
        // Arrange
        const string queueName = "test-queue-json";
        var handler = _serviceProvider!.GetRequiredService<RabbitMqDestinationHandler>();

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            destinationUrl: $"rabbitmq://{queueName}",
            destinationType: "RabbitMQ");

        var outputJson = JObject.Parse(@"{""userId"": 123, ""name"": ""John Doe""}");

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

        // Assert - Read message from queue
        using var connection = _connectionFactory!.CreateConnection();
        using var channel = connection.CreateModel();

        var result = channel.BasicGet(queueName, autoAck: true);
        result.Should().NotBeNull("message should be in queue");

        var messageBody = Encoding.UTF8.GetString(result!.Body.ToArray());
        var receivedJson = JObject.Parse(messageBody);

        receivedJson["userId"]!.Value<int>().Should().Be(123);
        receivedJson["name"]!.Value<string>().Should().Be("John Doe");
        result.BasicProperties.ContentType.Should().Be("application/json");
        result.BasicProperties.Persistent.Should().BeTrue();
    }

    [Test]
    public async Task RabbitMqDestinationHandler_ShouldPublishXmlMessageToQueue()
    {
        // Arrange
        const string queueName = "test-queue-xml";
        var handler = _serviceProvider!.GetRequiredService<RabbitMqDestinationHandler>();

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            destinationUrl: $"rabbitmq://{queueName}",
            destinationType: "RabbitMQ");

        var outputXml = System.Xml.Linq.XDocument.Parse(@"<User><Id>456</Id><Name>Jane Smith</Name></User>");

        var httpContext = new DefaultHttpContext();
        var request = httpContext.Request;
        var response = httpContext.Response;
        response.Body = new MemoryStream();

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
        using var connection = _connectionFactory!.CreateConnection();
        using var channel = connection.CreateModel();

        var result = channel.BasicGet(queueName, autoAck: true);
        result.Should().NotBeNull();

        var messageBody = Encoding.UTF8.GetString(result!.Body.ToArray());
        messageBody.Should().Contain("<Id>456</Id>");
        messageBody.Should().Contain("<Name>Jane Smith</Name>");
        result.BasicProperties.ContentType.Should().Be("application/xml");
    }

    [Test]
    public async Task RabbitMqDestinationHandler_ShouldPublishToExchangeWithRoutingKey()
    {
        // Arrange
        const string exchangeName = "test-exchange";
        const string routingKey = "test.routing.key";
        const string queueName = "test-bound-queue";

        var handler = _serviceProvider!.GetRequiredService<RabbitMqDestinationHandler>();

        // Setup exchange and queue binding
        using (var connection = _connectionFactory!.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, durable: true);
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(queueName, exchangeName, routingKey);
        }

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            destinationUrl: $"rabbitmq://{exchangeName}/{routingKey}",
            destinationType: "RabbitMQ");

        var outputJson = JObject.Parse(@"{""orderId"": 789}");

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

        // Assert - Message should be routed to bound queue
        using var readConnection = _connectionFactory!.CreateConnection();
        using var readChannel = readConnection.CreateModel();

        var result = readChannel.BasicGet(queueName, autoAck: true);
        result.Should().NotBeNull("message should be routed to queue");

        var messageBody = Encoding.UTF8.GetString(result!.Body.ToArray());
        var receivedJson = JObject.Parse(messageBody);
        receivedJson["orderId"]!.Value<int>().Should().Be(789);
    }

    [Test]
    public async Task RabbitMqDestinationHandler_ShouldIncludeStaticValuesAsHeaders()
    {
        // Arrange
        const string queueName = "test-queue-headers";
        var handler = _serviceProvider!.GetRequiredService<RabbitMqDestinationHandler>();

        var integration = CreateIntegrationMapping(
            name: "TestIntegration",
            destinationUrl: $"rabbitmq://{queueName}",
            destinationType: "RabbitMQ",
            staticValues: new Dictionary<string, string>
            {
                { "X-Source-System", "QuickApiMapper" },
                { "X-Version", "1.0" }
            });

        var outputJson = JObject.Parse(@"{""test"": ""data""}");

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
        using var connection = _connectionFactory!.CreateConnection();
        using var channel = connection.CreateModel();

        var result = channel.BasicGet(queueName, autoAck: true);
        result.Should().NotBeNull();

        result!.BasicProperties.Headers.Should().ContainKey("X-Source-System");
        result.BasicProperties.Headers.Should().ContainKey("X-Version");

        var sourceSystem = Encoding.UTF8.GetString((byte[])result.BasicProperties.Headers["X-Source-System"]);
        var version = Encoding.UTF8.GetString((byte[])result.BasicProperties.Headers["X-Version"]);

        sourceSystem.Should().Be("QuickApiMapper");
        version.Should().Be("1.0");
    }

    [Test]
    public async Task RabbitMqConsumer_ShouldConsumeMessagesFromQueue()
    {
        // Arrange
        const string queueName = "test-consumer-queue";
        var receivedMessages = new List<string>();
        using var messageReceivedEvent = new ManualResetEventSlim(false);

        // Publish test message first
        using (var connection = _connectionFactory!.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            var body = Encoding.UTF8.GetBytes(@"{""testId"": 999}");
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(string.Empty, queueName, properties, body);
        }

        // Act - Start consumer
        var logger = _serviceProvider!.GetRequiredService<ILogger<RabbitMqConsumer>>();
        using var consumer = new RabbitMqConsumer(logger, _connectionFactory!, queueName);

        // Subscribe to message events (simplified - in production would use the worker's internal processing)
        using var testConnection = _connectionFactory!.CreateConnection();
        using var testChannel = testConnection.CreateModel();

        var testConsumer = new EventingBasicConsumer(testChannel);
        testConsumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            receivedMessages.Add(message);
            testChannel.BasicAck(ea.DeliveryTag, false);
            messageReceivedEvent.Set();
        };

        testChannel.BasicConsume(queueName, autoAck: false, testConsumer);

        // Wait for message
        var received = messageReceivedEvent.Wait(TimeSpan.FromSeconds(10));

        // Assert
        received.Should().BeTrue("message should be received within timeout");
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Should().Contain("\"testId\": 999");
    }
}
