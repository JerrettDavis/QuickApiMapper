using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class MappingEngineTests
{
    private ServiceProvider? _serviceProvider;
    private IMappingEngineFactory? MappingEngineFactory => _serviceProvider?.GetRequiredService<IMappingEngineFactory>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static string BasePath => TestContext.CurrentContext.TestDirectory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();

        // Use the new centralized service registration
        services.AddQuickApiMapper();

        // Add additional test-specific services if needed
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        _serviceProvider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task CustomerIntegration_Mapping_Produces_Expected_Output()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var inputJson = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "CustomerIntegration", "CustomerIntegration-Input.json"));
        var expectedXmlString = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "CustomerIntegration", "CustomerIntegration-ExpectedOutput.xml"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null, "Config should not be null");
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");
        var inputJObject = JObject.Parse(inputJson);
        var tnsNs = integration.StaticValues?.FirstOrDefault(x => x.Key == "TnsNamespace").Value ?? "";
        XNamespace tns = tnsNs;
        var outputXml = new XDocument(new XElement(tns + "root"));
        var logger = new TestLogger();

        // Act - Use the new generic mapping engine
        var engine = MappingEngineFactory!.CreateEngine<JObject, XDocument>();
        var result = await engine.ApplyMappingAsync(
            integration.Mapping!,
            inputJObject,
            outputXml,
            integration.StaticValues,
            globalStatics: integration.StaticValues,
            serviceProvider: _serviceProvider
        );

        // Assert - Check the mapping was successful
        Assert.That(result.IsSuccess, Is.True, $"Mapping should be successful. Error: {result.ErrorMessage}");

        // Assert - compare ignoring whitespace and attribute order
        var actualNormalized = NormalizeXml(outputXml.ToString());
        var expectedNormalized = NormalizeXml(expectedXmlString);
        // Log the actual output for debugging
        TestContext.Out.WriteLine($"Actual Output:\n{actualNormalized}");
        // Print mapping logs
        TestContext.Out.WriteLine("Mapping logs:");
        foreach (var log in logger.Logs)
            TestContext.Out.WriteLine(log);
        Assert.That(actualNormalized, Is.EqualTo(expectedNormalized));
    }

    [Test]
    public async Task VendorIntegration_Mapping_Produces_Expected_Output()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "VendorIntegration", "VendorIntegration-Config.json"));
        var inputJson = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "VendorIntegration", "VendorIntegration-Input.json"));
        var expectedXmlString = File.ReadAllText(Path.Combine(BasePath, "Test_Data", "VendorIntegration", "VendorIntegration-ExpectedOutput.xml"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null, "Config should not be null");
        var integration = config.Mappings!.First(m => m.Name == "VendorIntegration");
        var inputJObject = JObject.Parse(inputJson);
        var tnsNs = integration.StaticValues?.FirstOrDefault(x => x.Key == "TnsNamespace").Value ?? "";
        XNamespace tns = tnsNs;
        var outputXml = new XDocument(new XElement(tns + "root"));
        var logger = new TestLogger();

        // Act - Use the new generic mapping engine
        var engine = MappingEngineFactory!.CreateEngine<JObject, XDocument>();
        var result = await engine.ApplyMappingAsync(
            integration.Mapping!,
            inputJObject,
            outputXml,
            integration.StaticValues,
            globalStatics: config.StaticValues,
            serviceProvider: _serviceProvider
        );

        // Assert - Check the mapping was successful
        Assert.That(result.IsSuccess, Is.True, $"Mapping should be successful. Error: {result.ErrorMessage}");

        // Assert - compare ignoring whitespace and attribute order
        var actualNormalized = NormalizeXml(outputXml.ToString());
        var expectedNormalized = NormalizeXml(expectedXmlString);
        await TestContext.Out.WriteLineAsync($"Actual Output:\n{actualNormalized}");
        // Print mapping logs
        await TestContext.Out.WriteLineAsync("Mapping logs:");
        foreach (var log in logger.Logs)
            await TestContext.Out.WriteLineAsync(log);
        Assert.That(actualNormalized, Is.EqualTo(expectedNormalized));
    }

    [Test]
    public async Task Mapping_OnlyAttributes_Produces_EmptyRootElement()
    {
        // Arrange: Only attribute mappings, no element value mappings
        var mappings = new List<FieldMapping>
        {
            new("$$.Alias", "/root/session/@alias"),
            new("$$.Email", "/root/session/@email")
        };
        var statics = new Dictionary<string, string> { { "Alias", "foo" }, { "Email", "bar" } };
        var outputXml = new XDocument(new XElement("root"));

        // Act - Use the new generic mapping engine
        var engine = MappingEngineFactory!.CreateEngine<JObject, XDocument>();
        var result = await engine.ApplyMappingAsync(
            mappings,
            new JObject(), // Empty JSON source
            outputXml,
            statics,
            serviceProvider: _serviceProvider
        );

        // Assert - Check the mapping was successful
        Assert.That(result.IsSuccess, Is.True, $"Mapping should be successful. Error: {result.ErrorMessage}");

        // Assert: The root should have a session child with alias and email attributes, but no value
        var sessionElem = outputXml.Root?.Element("session");
        Assert.That(sessionElem, Is.Not.Null, "Session element should exist");
        Assert.Multiple(() =>
        {
            Assert.That(sessionElem?.Attribute("alias")?.Value, Is.EqualTo("foo"));
            Assert.That(sessionElem?.Attribute("email")?.Value, Is.EqualTo("bar"));
            // The root element should not be empty
            Assert.That(outputXml.Root?.HasElements, Is.True, "Root should not be empty");
        });
    }

    [Test]
    public async Task Mapping_OnlyRootAttributes_Produces_EmptyRootElement()
    {
        // Arrange: Only root attribute mappings, no child elements or values
        var mappings = new List<FieldMapping>
        {
            new("$$.Alias", "/root/@alias"),
            new("$$.Email", "/root/@email")
        };
        var statics = new Dictionary<string, string> { { "Alias", "foo" }, { "Email", "bar" } };
        var outputXml = new XDocument(new XElement("root"));

        // Act - Use the new generic mapping engine
        var engine = MappingEngineFactory!.CreateEngine<JObject, XDocument>();
        var result = await engine.ApplyMappingAsync(
            mappings,
            new JObject(), // Empty JSON source
            outputXml,
            statics,
            serviceProvider: _serviceProvider
        );

        // Assert - Check the mapping was successful
        Assert.That(result.IsSuccess, Is.True, $"Mapping should be successful. Error: {result.ErrorMessage}");

        // Assert: The root should have alias and email attributes, but no value or children
        Assert.That(outputXml.Root?.Attribute("alias")?.Value, Is.EqualTo("foo"));
        Assert.That(outputXml.Root?.Attribute("email")?.Value, Is.EqualTo("bar"));
        // The root element should still be considered empty (no children, no value)
        Assert.That(outputXml.Root?.HasElements, Is.False, "Root should have no children");
        Assert.That(string.IsNullOrEmpty(outputXml.Root?.Value), Is.True, "Root should have no value");
    }

    private static string NormalizeXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        // Canonicalize (remove insignificant whitespace)
        return doc.ToString(SaveOptions.DisableFormatting).Replace("\r", "").Replace("\n", "").Trim();
    }
}

// Add a simple test logger
public class TestLogger : ILogger
{
    public List<string> Logs { get; } = [];
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        Logs.Add(msg);
        TestContext.Out.WriteLine(msg); // Write to test output immediately
    }
}