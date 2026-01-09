using System.Text.Json;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class OpenApiDocumentGeneratorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    [Test]
    public void SynthesizeJsonSchema_Generates_Correct_Structure_For_CustomerIntegration()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check for expected top-level and nested properties
        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);

        // Top-level properties should include interface, direction, sender, receiver, customerinfo
        Assert.That(props["interface"], Is.Not.Null, "interface should be present");
        Assert.That(props["direction"], Is.Not.Null, "direction should be present");
        Assert.That(props["sender"], Is.Not.Null, "sender should be present");
        Assert.That(props["receiver"], Is.Not.Null, "receiver should be present");
        Assert.That(props["sender_message_id"], Is.Not.Null, "sender_message_id should be present");
        
        // Check customerinfo array structure
        Assert.That(props["customerinfo"], Is.Not.Null, "customerinfo should be present");
        var customerinfo = props["customerinfo"] as JObject;
        Assert.That(customerinfo?["type"]?.ToString(), Is.EqualTo("array"));
        
        var customerinfoItems = customerinfo?["items"]?["properties"] as JObject;
        Assert.That(customerinfoItems, Is.Not.Null);
        Assert.Multiple(() =>
        {

            // Check customer-level properties
            Assert.That(customerinfoItems["customer_id"], Is.Not.Null, "customer_id should be present");
            Assert.That(customerinfoItems["customer_name"], Is.Not.Null, "customer_name should be present");

            // Check nested arrays
            Assert.That(customerinfoItems["address"], Is.Not.Null, "address array should be present");
            Assert.That(customerinfoItems["communication"], Is.Not.Null, "communication array should be present");
            Assert.That(customerinfoItems["invoice"], Is.Not.Null, "invoice array should be present");
            Assert.That(customerinfoItems["sales"], Is.Not.Null, "sales array should be present");
        });

        // Verify they are arrays
        Assert.That(customerinfoItems["address"]?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(customerinfoItems["communication"]?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(customerinfoItems["invoice"]?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(customerinfoItems["sales"]?["type"]?.ToString(), Is.EqualTo("array"));
    }

    [Test]
    public void SynthesizeJsonSchema_Contains_Nested_Address_Properties()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check nested address properties
        var addressItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["address"]?["items"]?["properties"] as JObject;
        Assert.That(addressItems, Is.Not.Null);
        
        // Check address fields based on the mapping configuration
        Assert.That(addressItems["address_1"], Is.Not.Null, "address_1 should be present");
        Assert.That(addressItems["city"], Is.Not.Null, "city should be present");
        Assert.That(addressItems["country_code"], Is.Not.Null, "country_code should be present");
        Assert.That(addressItems["state"], Is.Not.Null, "state should be present");
        Assert.That(addressItems["zip_code"], Is.Not.Null, "zip_code should be present");
        Assert.That(addressItems["address_type"], Is.Not.Null, "address_type should be present");
    }

    [Test]
    public void SynthesizeJsonSchema_Contains_Communication_Properties()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check communication properties
        var commItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["communication"]?["items"]?["properties"] as JObject;
        Assert.That(commItems, Is.Not.Null);
        
        // Should have comm_type and comm_value based on filter expressions
        Assert.That(commItems["comm_type"], Is.Not.Null, "comm_type should be present");
        Assert.That(commItems["comm_value"], Is.Not.Null, "comm_value should be present");
    }

    [Test]
    public void SynthesizeJsonSchema_Contains_Invoice_And_Sales_Properties()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check invoice properties
        var invoiceItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["invoice"]?["items"]?["properties"] as JObject;
        Assert.That(invoiceItems, Is.Not.Null);
        Assert.That(invoiceItems["credit_limit"], Is.Not.Null, "credit_limit should be present");
        Assert.That(invoiceItems["currency"], Is.Not.Null, "currency should be present");
        Assert.That(invoiceItems["payment_terms"], Is.Not.Null, "payment_terms should be present");
        
        // Assert - check sales properties
        var salesItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["sales"]?["items"]?["properties"] as JObject;
        Assert.That(salesItems, Is.Not.Null);
        Assert.That(salesItems["node"], Is.Not.Null, "node should be present");
    }

    [Test]
    public void SynthesizeJsonSchema_Infers_Property_Types_Correctly()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - all properties should be strings by default
        var invoiceItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["invoice"]?["items"]?["properties"] as JObject;
        Assert.That(invoiceItems?["credit_limit"]?["type"]?.ToString(), Is.EqualTo("string"), "credit_limit should be string type");
        
        // Check that regular string fields are string type
        var customerinfoItems = schema["properties"]?["customerinfo"]?["items"]?["properties"] as JObject;
        Assert.That(customerinfoItems?["customer_id"]?["type"]?.ToString(), Is.EqualTo("string"), "customer_id should be string type");
        
        // Check that sender_message_id is nullable
        var topLevelProps = schema["properties"] as JObject;
        Assert.That(topLevelProps?["sender_message_id"]?["nullable"]?.Value<bool>(), Is.True, "sender_message_id should be nullable");
    }

    [Test]
    public void GenerateOpenApiDocument_Contains_Expected_Endpoint_And_Schema()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var generator = new OpenApiDocumentGenerator(config);

        // Act
        var doc = generator.GenerateOpenApiDocument();

        // Assert
        var paths = doc["paths"] as JObject;
        Assert.That(paths, Is.Not.Null);
        Assert.That(paths["/CustomerIntegration"], Is.Not.Null);
        
        var post = paths["/CustomerIntegration"]?["post"] as JObject;
        Assert.That(post, Is.Not.Null);
        Assert.That(post["operationId"]?.ToString(), Is.EqualTo("CustomerIntegration"));
        
        var requestBody = post["requestBody"]?["content"]?["application/json"]?["schema"] as JObject;
        Assert.That(requestBody, Is.Not.Null);
        
        // Should contain customerinfo as array
        var props = requestBody["properties"] as JObject;
        Assert.That(props, Is.Not.Null);
        Assert.That(props["customerinfo"]?["type"]?.ToString(), Is.EqualTo("array"));
    }

    [Test]
    public void SynthesizeJsonSchema_DoesNotCreateInvalidPropertyNames()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check that invalid property names are not created
        var schemaString = schema.ToString();
        
        // These should NOT appear in the schema
        Assert.That(schemaString, Does.Not.Contain("comm_type == 'Email')]"), 
            "Invalid property name from filter expression should not appear");
        Assert.That(schemaString, Does.Not.Contain("comm_type == 'Phone')]"), 
            "Invalid property name from filter expression should not appear");
        Assert.That(schemaString, Does.Not.Contain("address_type == 'Invoice')]"), 
            "Invalid property name from filter expression should not appear");
        
        // These SHOULD appear in the schema
        Assert.That(schemaString, Does.Contain("comm_type"), 
            "Filter field should be extracted and included");
        Assert.That(schemaString, Does.Contain("address_type"), 
            "Filter field should be extracted and included");
        Assert.That(schemaString, Does.Contain("comm_value"), 
            "Property after filter should be included");
        Assert.That(schemaString, Does.Contain("address_1"), 
            "Property after filter should be included");
    }

    [Test]
    public void SynthesizeJsonSchema_IncludesAllTopLevelProperties()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check that all top-level properties from simple paths are included
        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);
        
        // These come from paths like "$.interface", "$.direction", etc.
        Assert.That(props["interface"], Is.Not.Null, "interface should be present");
        Assert.That(props["direction"], Is.Not.Null, "direction should be present");
        Assert.That(props["sender"], Is.Not.Null, "sender should be present");
        Assert.That(props["sender_message_id"], Is.Not.Null, "sender_message_id should be present");
        Assert.That(props["receiver"], Is.Not.Null, "receiver should be present");
        Assert.That(props["customerinfo"], Is.Not.Null, "customerinfo should be present");
        
        // sender_message_id should be nullable
        Assert.That(props["sender_message_id"]?["nullable"]?.Value<bool>(), Is.True, 
            "sender_message_id should be nullable");
    }

    [Test]
    public void SynthesizeJsonSchema_CreatesProperArrayStructures()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - verify array structures are created correctly
        var customerinfoArray = schema["properties"]?["customerinfo"] as JObject;
        Assert.That(customerinfoArray?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(customerinfoArray?["items"]?["type"]?.ToString(), Is.EqualTo("object"));
        
        var customerProps = customerinfoArray?["items"]?["properties"] as JObject;
        Assert.That(customerProps, Is.Not.Null);
        
        // Check nested arrays
        var addressArray = customerProps["address"] as JObject;
        Assert.That(addressArray?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(addressArray?["items"]?["type"]?.ToString(), Is.EqualTo("object"));
        
        var commArray = customerProps["communication"] as JObject;
        Assert.That(commArray?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(commArray?["items"]?["type"]?.ToString(), Is.EqualTo("object"));
        
        var invoiceArray = customerProps["invoice"] as JObject;
        Assert.That(invoiceArray?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(invoiceArray?["items"]?["type"]?.ToString(), Is.EqualTo("object"));
        
        var salesArray = customerProps["sales"] as JObject;
        Assert.That(salesArray?["type"]?.ToString(), Is.EqualTo("array"));
        Assert.That(salesArray?["items"]?["type"]?.ToString(), Is.EqualTo("object"));
    }

    [Test]
    public void SynthesizeJsonSchema_FilterFieldsAreAddedToCorrectArrayItems()
    {
        // Arrange
        var configJson = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Test_Data", "CustomerIntegration", "CustomerIntegration-Config.json"));
        var config = JsonSerializer.Deserialize<ApiMappingConfig>(configJson, JsonOptions);
        Assert.That(config, Is.Not.Null);
        var integration = config.Mappings!.First(m => m.Name == "CustomerIntegration");

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(integration.Mapping);

        // Assert - check that filter fields are added to the correct array item properties
        var commItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["communication"]?["items"]?["properties"] as JObject;
        Assert.That(commItems, Is.Not.Null);
        Assert.That(commItems["comm_type"], Is.Not.Null, "comm_type should be present from filter");
        Assert.That(commItems["comm_type"]?["type"]?.ToString(), Is.EqualTo("string"));
        
        var addressItems = schema["properties"]?["customerinfo"]?["items"]?["properties"]?["address"]?["items"]?["properties"] as JObject;
        Assert.That(addressItems, Is.Not.Null);
        Assert.That(addressItems["address_type"], Is.Not.Null, "address_type should be present from filter");
        Assert.That(addressItems["address_type"]?["type"]?.ToString(), Is.EqualTo("string"));
    }

    [Test]
    public void SynthesizeJsonSchema_HandlesComplexFilterExpressions()
    {
        // Arrange - create a mapping with filter expressions
        var mappings = new List<FieldMapping>
        {
            new("$.items[?(@.category == 'electronics')].name", ""),
            new("$.items[?(@.status == 'active')].description", ""),
            new("$.users[?(@.role == 'admin')].permissions[0].name", "")
        };

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(mappings);

        // Assert - check that filter expressions are handled
        var itemsArray = schema["properties"]?["items"] as JObject;
        Assert.That(itemsArray?["type"]?.ToString(), Is.EqualTo("array"));
        
        var itemProps = itemsArray?["items"]?["properties"] as JObject;
        Assert.That(itemProps, Is.Not.Null);
        
        // Should extract 'category' from the first filter expression
        Assert.That(itemProps["category"], Is.Not.Null, "category should be extracted from filter");
        Assert.That(itemProps["status"], Is.Not.Null, "status should be extracted from second filter");
        Assert.That(itemProps["name"], Is.Not.Null, "name should be present as leaf property");
        Assert.That(itemProps["description"], Is.Not.Null, "description should be present as leaf property");
        
        // Check nested structure for users
        var usersArray = schema["properties"]?["users"] as JObject;
        Assert.That(usersArray?["type"]?.ToString(), Is.EqualTo("array"));
        
        var userProps = usersArray?["items"]?["properties"] as JObject;
        Assert.That(userProps, Is.Not.Null);
        Assert.That(userProps["role"], Is.Not.Null, "role should be extracted from filter");
        
        var permissionsArray = userProps["permissions"] as JObject;
        Assert.That(permissionsArray?["type"]?.ToString(), Is.EqualTo("array"));
    }

    [Test]
    public void Debug_TopLevel_Field_Parsing()
    {
        // Arrange - create mappings that mirror the actual config
        var mappings = new List<FieldMapping>
        {
            new("$.interface", ""),
            new("$.direction", ""),
            new("$.sender", ""),
            new("$.sender_message_id", ""),
            new("$.receiver", ""),
            new("$.customerinfo[0].customer_id", "/some/destination")
        };

        // Act
        var schema = OpenApiDocumentGenerator.SynthesizeJsonSchema(mappings);

        // Assert - debug what we actually get
        Console.WriteLine("Generated schema:");
        Console.WriteLine(schema.ToString());
        
        var props = schema["properties"] as JObject;
        Assert.That(props, Is.Not.Null);
        
        // These should be present at the top level
        Assert.That(props["interface"], Is.Not.Null, "interface should be present");
        Assert.That(props["direction"], Is.Not.Null, "direction should be present");
        Assert.That(props["sender"], Is.Not.Null, "sender should be present");
        Assert.That(props["sender_message_id"], Is.Not.Null, "sender_message_id should be present");
        Assert.That(props["receiver"], Is.Not.Null, "receiver should be present");
        Assert.That(props["customerinfo"], Is.Not.Null, "customerinfo should be present");
    }
}
