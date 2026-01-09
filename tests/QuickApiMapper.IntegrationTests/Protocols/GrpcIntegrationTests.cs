using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Extensions.gRPC.Resolvers;
using QuickApiMapper.Extensions.gRPC.Writers;

namespace QuickApiMapper.IntegrationTests.Protocols;

/// <summary>
/// Integration tests for gRPC protocol extension.
/// Tests resolver and writer functionality with Protobuf messages.
/// </summary>
[TestFixture]
public class GrpcIntegrationTests
{
    private GrpcSourceResolver? _sourceResolver;
    private GrpcDestinationWriter? _destinationWriter;
    private ILoggerFactory? _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _sourceResolver = new GrpcSourceResolver(_loggerFactory.CreateLogger<GrpcSourceResolver>());
        _destinationWriter = new GrpcDestinationWriter(_loggerFactory.CreateLogger<GrpcDestinationWriter>());
    }

    [TearDown]
    public void TearDown()
    {
        _loggerFactory?.Dispose();
    }

    [Test]
    public void GrpcSourceResolver_ShouldSupportGrpcAndProtobufTokens()
    {
        // Assert
        _sourceResolver!.SupportedTokens.Should().Contain("$grpc");
        _sourceResolver.SupportedTokens.Should().Contain("$protobuf");
        _sourceResolver.SupportedTokens.Should().Contain("$static");
    }

    [Test]
    public void GrpcSourceResolver_ShouldResolveFieldFromProtobufMessage()
    {
        // Arrange
        var message = new StringValue { Value = "test-value" };

        // Act
        var result = _sourceResolver!.Resolve("Value", message);

        // Assert
        result.Should().Be("test-value");
    }

    [Test]
    public void GrpcSourceResolver_ShouldResolveNestedFieldFromProtobufMessage()
    {
        // Arrange
        var message = Struct.Parser.ParseJson(@"{
            ""user"": {
                ""name"": ""John Doe"",
                ""id"": 123
            }
        }");

        // Act - Access nested field using dot notation
        var userName = _sourceResolver!.Resolve("user.name", message);
        var userId = _sourceResolver!.Resolve("user.id", message);

        // Assert
        userName.Should().NotBeNull();
        userId.Should().NotBeNull();
    }

    [Test]
    public void GrpcSourceResolver_ShouldResolveStaticValue()
    {
        // Arrange
        var message = new StringValue { Value = "test" };
        var staticValues = new Dictionary<string, string>
        {
            { "API_KEY", "secret-key-123" },
            { "VERSION", "1.0" }
        };

        // Act
        var apiKey = _sourceResolver!.Resolve("$static:API_KEY", message, staticValues);
        var version = _sourceResolver!.Resolve("$static:VERSION", message, staticValues);

        // Assert
        apiKey.Should().Be("secret-key-123");
        version.Should().Be("1.0");
    }

    [Test]
    public void GrpcSourceResolver_ShouldReturnNullForNonExistentField()
    {
        // Arrange
        var message = new StringValue { Value = "test" };

        // Act
        var result = _sourceResolver!.Resolve("NonExistentField", message);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GrpcSourceResolver_CanResolve_ShouldReturnTrueForValidPaths()
    {
        // Assert
        _sourceResolver!.CanResolve("Value").Should().BeTrue();
        _sourceResolver.CanResolve("$grpc:FieldName").Should().BeTrue();
        _sourceResolver.CanResolve("$protobuf:user.name").Should().BeTrue();
        _sourceResolver.CanResolve("$static:API_KEY").Should().BeTrue();
    }

    [Test]
    public void GrpcSourceResolver_CanResolve_ShouldReturnFalseForUnsupportedTokens()
    {
        // Assert
        _sourceResolver!.CanResolve("$json:path").Should().BeFalse();
        _sourceResolver.CanResolve("$xml:xpath").Should().BeFalse();
    }

    [Test]
    public void GrpcDestinationWriter_ShouldSupportGrpcAndProtobufTokens()
    {
        // Assert
        _destinationWriter!.SupportedTokens.Should().Contain("$grpc");
        _destinationWriter.SupportedTokens.Should().Contain("$protobuf");
    }

    [Test]
    public void GrpcDestinationWriter_ShouldWriteSimpleFieldToProtobufMessage()
    {
        // Arrange
        var message = new StringValue();

        // Act
        var success = _destinationWriter!.Write("Value", "new-value", message);

        // Assert
        success.Should().BeTrue();
        message.Value.Should().Be("new-value");
    }

    [Test]
    public void GrpcDestinationWriter_ShouldWriteNestedFieldToStructMessage()
    {
        // Arrange
        var message = new Struct();

        // Act - Write to nested structure
        var success = _destinationWriter!.Write("user.name", "Jane Doe", message);

        // Assert
        success.Should().BeTrue();
        // Note: Struct is a complex type; verify the structure was created
        message.Fields.Should().ContainKey("user");
    }

    [Test]
    public void GrpcDestinationWriter_ShouldHandleNullValue()
    {
        // Arrange
        var message = new StringValue { Value = "original" };

        // Act
        var success = _destinationWriter!.Write("Value", null, message);

        // Assert
        success.Should().BeTrue();
        message.Value.Should().BeEmpty(); // Protobuf defaults empty string for null
    }

    [Test]
    public void GrpcDestinationWriter_CanWrite_ShouldReturnTrueForValidPaths()
    {
        // Assert
        _destinationWriter!.CanWrite("Value").Should().BeTrue();
        _destinationWriter.CanWrite("$grpc:FieldName").Should().BeTrue();
        _destinationWriter.CanWrite("$protobuf:user.name").Should().BeTrue();
    }

    [Test]
    public void GrpcDestinationWriter_CanWrite_ShouldReturnFalseForUnsupportedTokens()
    {
        // Assert
        _destinationWriter!.CanWrite("$json:path").Should().BeFalse();
        _destinationWriter.CanWrite("$xml:xpath").Should().BeFalse();
    }

    [Test]
    public void GrpcResolverAndWriter_ShouldWorkTogether()
    {
        // Arrange
        var sourceMessage = new StringValue { Value = "source-data" };
        var destinationMessage = new StringValue();

        // Act - Resolve from source
        var resolvedValue = _sourceResolver!.Resolve("Value", sourceMessage);

        // Write to destination
        var writeSuccess = _destinationWriter!.Write("Value", resolvedValue, destinationMessage);

        // Assert
        writeSuccess.Should().BeTrue();
        destinationMessage.Value.Should().Be("source-data");
    }

    [Test]
    public void GrpcMessages_ShouldSupportTimestampType()
    {
        // Arrange
        var sourceMessage = new Timestamp
        {
            Seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nanos = 0
        };

        // Act
        var seconds = _sourceResolver!.Resolve("Seconds", sourceMessage);
        var nanos = _sourceResolver!.Resolve("Nanos", sourceMessage);

        // Assert
        seconds.Should().NotBeNull();
        nanos.Should().NotBeNull();
        long.Parse(seconds!).Should().BeGreaterThan(0);
    }

    [Test]
    public void GrpcMessages_ShouldSupportDurationType()
    {
        // Arrange
        var duration = new Duration
        {
            Seconds = 3600, // 1 hour
            Nanos = 500000000 // 0.5 seconds
        };

        var destinationMessage = new Duration();

        // Act
        var secondsValue = _sourceResolver!.Resolve("Seconds", duration);
        _destinationWriter!.Write("Seconds", secondsValue, destinationMessage);

        // Assert
        destinationMessage.Seconds.Should().Be(3600);
    }

    [Test]
    public void GrpcMessages_ShouldHandleEmptyMessage()
    {
        // Arrange
        var emptyMessage = new Empty();

        // Act
        var result = _sourceResolver!.Resolve("SomeField", emptyMessage);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void GrpcMessages_ShouldHandleAnyType()
    {
        // Arrange
        var originalMessage = new StringValue { Value = "packed-data" };
        var anyMessage = Any.Pack(originalMessage);

        // Act - Any type contains TypeUrl and Value fields
        var typeUrl = _sourceResolver!.Resolve("TypeUrl", anyMessage);

        // Assert
        typeUrl.Should().NotBeNullOrEmpty();
        typeUrl.Should().Contain("google.protobuf.StringValue");
    }
}
