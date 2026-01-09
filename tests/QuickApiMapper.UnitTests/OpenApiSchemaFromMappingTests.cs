using System.Text.Json;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class OpenApiSchemaFromMappingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Test]
    public void SynthesizeJsonSchema_Reflects_All_Fields_From_Mapping()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);
        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);

        // Top-level fields based on the mapping configuration
        var expectedTopLevels = new[] { "interface", "direction", "sender", "sender_message_id", "receiver", "customerinfo" };
        foreach (var field in expectedTopLevels)
        {
            Assert.That(props[field], Is.Not.Null, $"Top-level field '{field}' should be present in schema");
        }

        // Nested: customerinfo[]
        var customerinfo = props["customerinfo"] as JObject;
        Assert.That(customerinfo?["type"]?.ToString(), Is.EqualTo("array"));
        var customerinfoItems = customerinfo?["items"]?["properties"] as JObject;
        Assert.That(customerinfoItems, Is.Not.Null);
        
        // Check customer-level fields from the mapping
        Assert.That(customerinfoItems["customer_id"], Is.Not.Null, "customer_id should be present");
        Assert.That(customerinfoItems["customer_name"], Is.Not.Null, "customer_name should be present");
        Assert.That(customerinfoItems["address"], Is.Not.Null, "address should be present");
        Assert.That(customerinfoItems["communication"], Is.Not.Null, "communication should be present");
        Assert.That(customerinfoItems["invoice"], Is.Not.Null, "invoice should be present");
        Assert.That(customerinfoItems["sales"], Is.Not.Null, "sales should be present");

        // Deep nested: address[]
        var address = customerinfoItems["address"]?["items"]?["properties"] as JObject;
        Assert.That(address, Is.Not.Null);
        Assert.That(address["address_1"], Is.Not.Null, "address_1 should be present");
        Assert.That(address["city"], Is.Not.Null, "city should be present");
        Assert.That(address["zip_code"], Is.Not.Null, "zip_code should be present");
        Assert.That(address["country_code"], Is.Not.Null, "country_code should be present");
        Assert.That(address["state"], Is.Not.Null, "state should be present");
        Assert.That(address["address_type"], Is.Not.Null, "address_type should be present from filter");

        // Deep nested: communication[]
        var comm = customerinfoItems["communication"]?["items"]?["properties"] as JObject;
        Assert.That(comm, Is.Not.Null);
        Assert.That(comm["comm_type"], Is.Not.Null, "comm_type should be present from filter");
        Assert.That(comm["comm_value"], Is.Not.Null, "comm_value should be present");

        // Deep nested: invoice[]
        var invoice = customerinfoItems["invoice"]?["items"]?["properties"] as JObject;
        Assert.That(invoice, Is.Not.Null);
        Assert.That(invoice["credit_limit"], Is.Not.Null, "credit_limit should be present");
        Assert.That(invoice["currency"], Is.Not.Null, "currency should be present");
        Assert.That(invoice["payment_terms"], Is.Not.Null, "payment_terms should be present");

        // Deep nested: sales[]
        var sales = customerinfoItems["sales"]?["items"]?["properties"] as JObject;
        Assert.That(sales, Is.Not.Null);
        Assert.That(sales["node"], Is.Not.Null, "node should be present");
    }

    [Test]
    public void SynthesizeJsonSchema_Handles_Array_Filter_Expressions()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - verify that filter expressions like [?(@.comm_type == 'Email')] are properly handled
        var commItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["communication"]?["items"]?["properties"] as JObject;
        Assert.That(commItems, Is.Not.Null);
        
        // These should be present because of the filter expressions in the mapping
        Assert.That(commItems["comm_type"], Is.Not.Null, "comm_type should be inferred from filter expression");
        Assert.That(commItems["comm_value"], Is.Not.Null, "comm_value should be present");

        // Similarly for address filter expressions
        var addressItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["address"]?["items"]?["properties"] as JObject;
        Assert.That(addressItems, Is.Not.Null);
        Assert.That(addressItems["address_type"], Is.Not.Null, "address_type should be inferred from filter expression");
    }

    [Test]
    public void SynthesizeJsonSchema_Excludes_Static_Values()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);
        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);

        // Assert - static values (starting with $$) should not appear in the schema
        Assert.That(props["SessionAlias"], Is.Null, "Static values should not appear in schema");
        Assert.That(props["SessionEmail"], Is.Null, "Static values should not appear in schema");
        Assert.That(props["CustomerRequest"], Is.Null, "Static values should not appear in schema");
        Assert.That(props["Data"], Is.Null, "Static values should not appear in schema");
        Assert.That(props["def_priority"], Is.Null, "Static values should not appear in schema");
        Assert.That(props["PriceBook"], Is.Null, "Static values should not appear in schema");
    }

    [Test]
    public void SynthesizeJsonSchema_Generates_Valid_OpenAPI_Schema_Structure()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        Assert.Multiple(() =>
        {
            // Assert - verify the schema has the correct OpenAPI structure
            Assert.That(schema["type"]?.ToString(), Is.EqualTo("object"), "Root should be an object");
            Assert.That(schema["properties"], Is.Not.Null, "Should have properties");
        });

        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);
        
        // Check array structures are properly defined
        var customerinfo = props["customerinfo"] as JObject;
        Assert.Multiple(() =>
        {
            Assert.That(customerinfo!["type"]?.ToString(), Is.EqualTo("array"), "customerinfo should be array");
            Assert.That(customerinfo["items"], Is.Not.Null, "Array should have items definition");
        });
        Assert.Multiple(() =>
        {
            Assert.That(customerinfo!["items"]?["type"]?.ToString(), Is.EqualTo("object"), "Array items should be objects");
            Assert.That(customerinfo["items"]?["properties"], Is.Not.Null, "Array items should have properties");
        });
    }
}
