using System.Text.Json;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests.Infrastructure;

/// <summary>
/// Provides centralized test data management and loading utilities.
/// Eliminates duplication of configuration and test data loading across tests.
/// </summary>
public static class TestDataManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    private static readonly string BasePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data");
    
    /// <summary>
    /// Loads and deserializes a configuration file.
    /// </summary>
    /// <param name="folder">The folder containing the expected output file (e.g., "CustomerIntegration").</param>
    /// <param name="configFileName">The config file name (e.g., "CustomerIntegration-Config.json").</param>
    /// <returns>The deserialized configuration.</returns>
    public static ApiMappingConfig LoadConfig(string folder, string configFileName)
    {
        var configPath = Path.Combine(BasePath, folder, configFileName);
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}");
        }
        
        var configJson = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        
        return config ?? throw new InvalidOperationException($"Failed to deserialize config from {configFileName}");
    }
    
    /// <summary>
    /// Loads input JSON data for a test case.
    /// </summary>
    /// <param name="folder">The folder containing the expected output file (e.g., "CustomerIntegration").</param>
    /// <param name="inputFileName">The input file name (e.g., "CustomerIntegration-Input.json").</param>
    /// <returns>The parsed JObject.</returns>
    public static JObject LoadInputJson(string folder, string inputFileName)
    {
        var inputPath = Path.Combine(BasePath, folder, inputFileName);
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}");
        }
        
        var inputJson = File.ReadAllText(inputPath);
        return JObject.Parse(inputJson);
    }
    
    /// <summary>
    /// Loads expected output XML for a test case.
    /// </summary>
    /// <param name="folder">The folder containing the expected output file (e.g., "CustomerIntegration").</param>
    /// <param name="expectedFileName">The expected output file name (e.g., "CustomerIntegration-ExpectedOutput.xml").</param>
    /// <returns>The normalized expected XML string.</returns>
    public static string LoadExpectedXml(string folder, string expectedFileName)
    {
        var expectedPath = Path.Combine(BasePath, folder, expectedFileName);
        if (!File.Exists(expectedPath))
        {
            throw new FileNotFoundException($"Expected output file not found: {expectedPath}");
        }
        
        var expectedXml = File.ReadAllText(expectedPath);
        return NormalizeXml(expectedXml);
    }
    
    /// <summary>
    /// Creates a test data set for a specific integration test.
    /// </summary>
    /// <param name="testName">The test name (e.g., "CustomerIntegration", "VendorIntegration").</param>
    /// <returns>A complete test data set.</returns>
    public static TestDataSet CreateTestDataSet(string testName)
    {
        var config = LoadConfig(testName, $"{testName}-Config.json");
        var input = LoadInputJson(testName, $"{testName}-Input.json");
        var expected = LoadExpectedXml(testName, $"{testName}-ExpectedOutput.xml");
        var integration = config.Mappings?.First(m => m.Name == testName) 
                          ?? throw new InvalidOperationException($"Integration mapping not found for {testName}");
        
        return new TestDataSet(config, input, expected, integration);
    }
    
    /// <summary>
    /// Normalizes XML by removing whitespace and formatting for comparison.
    /// </summary>
    /// <param name="xml">The XML string to normalize.</param>
    /// <returns>The normalized XML string.</returns>
    public static string NormalizeXml(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.ToString(SaveOptions.DisableFormatting)
                  .Replace("\r", "")
                  .Replace("\n", "")
                  .Trim();
    }
}

/// <summary>
/// Represents a complete test data set for an integration test.
/// </summary>
/// <param name="Config">The full configuration.</param>
/// <param name="Input">The input JSON data.</param>
/// <param name="Expected">The expected normalized XML output.</param>
/// <param name="Integration">The specific integration mapping.</param>
public readonly record struct TestDataSet(
    ApiMappingConfig Config,
    JObject Input,
    string Expected,
    IntegrationMapping Integration);
