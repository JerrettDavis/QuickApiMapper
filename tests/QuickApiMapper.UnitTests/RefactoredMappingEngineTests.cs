using QuickApiMapper.Contracts;
using QuickApiMapper.UnitTests.Infrastructure;

namespace QuickApiMapper.UnitTests;

/// <summary>
/// Refactored mapping engine tests using the new centralized test infrastructure.
/// Demonstrates how the new utilities eliminate boilerplate and improve maintainability.
/// </summary>
[TestFixture]
public class RefactoredMappingEngineTests
{
    [Test]
    public void CustomerIntegration_Mapping_Produces_Expected_Output()
    {
        // Arrange & Act - All setup is now handled by TestHelpers
        var actualOutput = TestHelpers.ExecuteMappingTest("CustomerIntegration");
        
        // Assert - TestDataManager handles loading expected output
        var expectedOutput = TestDataManager.LoadExpectedXml("CustomerIntegration", "CustomerIntegration-ExpectedOutput.xml");
        TestHelpers.AssertXmlEquivalent(expectedOutput, actualOutput, "CustomerIntegration");
    }

    [Test]
    public void VendorIntegration_Mapping_Produces_Expected_Output()
    {
        // Arrange & Act - All setup is now handled by TestHelpers
        var actualOutput = TestHelpers.ExecuteMappingTest("VendorIntegration");
        
        // Assert - TestDataManager handles loading expected output
        var expectedOutput = TestDataManager.LoadExpectedXml("VendorIntegration", "VendorIntegration-ExpectedOutput.xml");
        TestHelpers.AssertXmlEquivalent(expectedOutput, actualOutput, "VendorIntegration");
    }

    [Test]
    public void Multiple_Test_Cases_Can_Be_Run_Parametrically()
    {
        // Arrange - Test cases can now be easily parameterized
        var testCases = new[] { "CustomerIntegration", "VendorIntegration" };
        
        foreach (var testCase in testCases)
        {
            // Act
            var actualOutput = TestHelpers.ExecuteMappingTest(testCase);
            
            // Assert
            var expectedOutput = TestDataManager.LoadExpectedXml(testCase, $"{testCase}-ExpectedOutput.xml");
            TestHelpers.AssertXmlEquivalent(expectedOutput, actualOutput, testCase);
        }
    }
}

/// <summary>
/// Example custom transformer for testing.
/// </summary>
public sealed class CustomTestTransformer : ITransformer
{
    public string Name => "customTest";
    
    public string Transform(string? input, IReadOnlyDictionary<string, string?>? args)
        => input?.Replace("test", "custom") ?? "";
}
