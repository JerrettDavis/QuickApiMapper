using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.Application.Transformers;
using QuickApiMapper.Contracts;
using Newtonsoft.Json.Linq;


namespace QuickApiMapper.UnitTests.Infrastructure;

/// <summary>
/// Provides common test utilities and assertion helpers.
/// Reduces boilerplate code and ensures consistent test patterns.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates an output XML document with the correct namespace for a test.
    /// </summary>
    /// <param name="integration">The integration mapping.</param>
    /// <returns>A properly initialized XML document.</returns>
    public static XDocument CreateOutputXml(IntegrationMapping integration)
    {
        var tnsNs = integration.StaticValues?.FirstOrDefault(x => x.Key == "TnsNamespace").Value ?? "";
        XNamespace tns = tnsNs;
        return new XDocument(new XElement(tns + "root"));
    }

    /// <summary>
    /// Executes a complete mapping test with all standard setup using the new generic mapping system.
    /// </summary>
    /// <param name="testName">The test name (e.g., "CustomerIntegration", "VendorIntegration").</param>
    /// <returns>The actual normalized XML output.</returns>
    public static async Task<string> ExecuteMappingTestAsync(string testName)
    {
        var testData = TestDataManager.CreateTestDataSet(testName);
        var mappingEngineFactory = TestServiceProvider.Instance.GetRequiredService<IMappingEngineFactory>();

        // Create output XML
        var outputXml = CreateOutputXml(testData.Integration);

        // Create the appropriate generic mapping engine (assuming JSON to XML)
        var engine = mappingEngineFactory.CreateEngine<JObject, XDocument>();

        // Execute mapping
        var result = await engine.ApplyMappingAsync(
            testData.Integration.Mapping ?? throw new InvalidOperationException("No mapping defined for test"),
            testData.Input,
            outputXml,
            testData.Integration.StaticValues,
            globalStatics: testData.Config.StaticValues,
            serviceProvider: TestServiceProvider.Instance
        );

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Mapping failed: {result.ErrorMessage}");
        }

        return TestDataManager.NormalizeXml(outputXml.ToString());
    }

    /// <summary>
    /// Synchronous wrapper for backward compatibility (use async version when possible).
    /// </summary>
    /// <param name="testName">The test name (e.g., "CustomerIntegration", "VendorIntegration").</param>
    /// <returns>The actual normalized XML output.</returns>
    public static string ExecuteMappingTest(string testName)
    {
        return ExecuteMappingTestAsync(testName).GetAwaiter().GetResult();
    }

    public static void AddTransformer(ITransformer transformer)
    {
        var registry = TestServiceProvider.Instance.GetRequiredService<ITransformerRegistry>();
        if (registry is TransformerRegistry transformerRegistry)
        {
            transformerRegistry.AddTransformer(transformer);
        }
        else
        {
            throw new InvalidOperationException("Test transformer registry is not of expected type");
        }
    }

    /// <summary>
    /// Asserts that two XML strings are equivalent (ignoring formatting).
    /// </summary>
    /// <param name="expected">The expected XML.</param>
    /// <param name="actual">The actual XML.</param>
    /// <param name="testName">The test name for error reporting.</param>
    public static void AssertXmlEquivalent(string expected, string actual, string testName)
    {
        var normalizedExpected = TestDataManager.NormalizeXml(expected);
        var normalizedActual = TestDataManager.NormalizeXml(actual);

        if (normalizedExpected != normalizedActual)
        {
            TestContext.Out.WriteLine($"=== {testName} Test Output Comparison ===");
            TestContext.Out.WriteLine($"Expected:\n{normalizedExpected}");
            TestContext.Out.WriteLine($"Actual:\n{normalizedActual}");

            Assert.That(normalizedActual, Is.EqualTo(normalizedExpected),
                $"{testName} mapping did not produce expected output");
        }
    }
}

/// <summary>
/// Test-specific transformer for boolean values.
/// </summary>
public sealed class TestBooleanTransformer : ITransformer
{
    public string Name => "testBoolean";

    public string Transform(string? input, IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return bool.TryParse(input, out var result) ? result ? "true" : "false" : input;
    }
}