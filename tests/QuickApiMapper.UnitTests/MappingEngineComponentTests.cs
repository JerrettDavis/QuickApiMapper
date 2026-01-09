using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Resolvers;
using QuickApiMapper.Application.Writers;
using QuickApiMapper.Contracts;
using QuickApiMapper.StandardTransformers;
using QuickApiMapper.CustomTransformers;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class MappingEngineComponentTests
{
    private ServiceProvider? _serviceProvider;
    private ILogger<XmlDestinationWriter>? _xmlLogger;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<ISourceResolver<IReadOnlyDictionary<string, string>>, StaticSourceResolver>();
        services.AddSingleton<ISourceResolver<JObject>, JsonSourceResolver>();
        services.AddSingleton<ISourceResolver<XDocument>, XmlSourceResolver>();
        
        // Register the new generic destination writers
        services.AddSingleton<IDestinationWriter<XDocument>, XmlDestinationWriter>();
        services.AddSingleton<IDestinationWriter<JObject>, JsonDestinationWriter>();
        
        _serviceProvider = services.BuildServiceProvider();
        _xmlLogger = _serviceProvider.GetRequiredService<ILogger<XmlDestinationWriter>>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void StaticSourceResolver_Resolves_Static_Value()
    {
        var resolver = new StaticSourceResolver();
        var statics = new Dictionary<string, string> { ["foo"] = "bar" };
        Assert.That(resolver.CanResolve("$$.foo"), Is.True);
        Assert.That(resolver.Resolve("$$.foo", statics, statics), Is.EqualTo("bar"));
        Assert.That(resolver.Resolve("$$.missing", statics, statics), Is.Null);
    }

    [Test]
    public void JsonSourceResolver_Resolves_JsonPath()
    {
        var resolver = new JsonSourceResolver();
        var json = JObject.Parse(@"{ ""user"": { ""name"": ""John"" } }");
        Assert.That(resolver.CanResolve("$.user.name"), Is.True);
        Assert.That(resolver.Resolve("$.user.name", json, null), Is.EqualTo("John"));
        Assert.That(resolver.Resolve("$.user.missing", json, null), Is.Null);
    }

    [Test]
    public void XmlSourceResolver_Resolves_XPath()
    {
        var resolver = new XmlSourceResolver();
        var xml = XDocument.Parse("<root><user><name>John</name></user></root>");
        Assert.That(resolver.CanResolve("xml:/root/user/name"), Is.True);
        Assert.That(resolver.Resolve("xml:/root/user/name", xml, null), Is.EqualTo("John"));
        Assert.That(resolver.Resolve("xml:/root/user/missing", xml, null), Is.Null);
    }

    [Test]
    public void XmlDestinationWriter_WritesElementValue()
    {
        var writer = new XmlDestinationWriter(_xmlLogger!);
        var xml = new XDocument(new XElement("root"));
        Assert.That(writer.CanWrite("/root/user/name"), Is.True);
        Assert.That(writer.Write("/root/user/name", "John", xml), Is.True);
        Assert.That(xml.Root?.Element("user")?.Element("name")?.Value, Is.EqualTo("John"));
    }

    [Test]
    public void XmlDestinationWriter_WritesAttributeValue()
    {
        var writer = new XmlDestinationWriter(_xmlLogger!);
        var xml = new XDocument(new XElement("root"));
        Assert.That(writer.CanWrite("/root/user/@name"), Is.True);
        Assert.That(writer.Write("/root/user/@name", "John", xml), Is.True);
        Assert.That(xml.Root?.Element("user")?.Attribute("name")?.Value, Is.EqualTo("John"));
    }

    [Test]
    public void JsonDestinationWriter_WritesValue()
    {
        var writer = new JsonDestinationWriter();
        var json = new JObject();
        Assert.That(writer.CanWrite("$.user.name"), Is.True);
        Assert.That(writer.Write("$.user.name", "John", json), Is.True);
        Assert.That(json["user"]?["name"]?.Value<string>(), Is.EqualTo("John"));
    }

    [Test]
    public void ToUpperTransformer_TransformsValue()
    {
        var transformer = new ToUpperTransformer();
        Assert.That(transformer.Name, Is.EqualTo("toUpper"));
        Assert.That(transformer.Transform("hello", null), Is.EqualTo("HELLO"));
        Assert.That(transformer.Transform(null, null), Is.EqualTo(""));
    }

    [Test]
    public void ToBooleanTransformer_TransformsValue()
    {
        var transformer = new ToBooleanTransformer();
        Assert.That(transformer.Name, Is.EqualTo("toBoolean"));
        Assert.That(transformer.Transform("true", null), Is.EqualTo("True"));
        Assert.That(transformer.Transform("false", null), Is.EqualTo("False"));
        Assert.That(transformer.Transform("invalid", null), Is.EqualTo("False"));
    }

    [Test]
    public void FormatPhoneTransformer_FormatsPhone()
    {
        var transformer = new FormatPhoneTransformer();
        Assert.That(transformer.Name, Is.EqualTo("formatPhone"));
        Assert.That(transformer.Transform("+123-456-7890", null), Is.EqualTo("1234567890"));
        Assert.That(transformer.Transform("invalid", null), Is.EqualTo("invalid"));
    }

    [Test]
    public void BooleanToYNTransformer_TransformsTrueToY()
    {
        var transformer = new BooleanToYNTransformer();
        Assert.That(transformer.Name, Is.EqualTo("booleanToYN"));
        Assert.That(transformer.Transform("true", null), Is.EqualTo("Y"));
        Assert.That(transformer.Transform("True", null), Is.EqualTo("Y"));
        Assert.That(transformer.Transform("TRUE", null), Is.EqualTo("Y"));
    }

    [Test]
    public void BooleanToYNTransformer_TransformsFalseToN()
    {
        var transformer = new BooleanToYNTransformer();
        Assert.That(transformer.Transform("false", null), Is.EqualTo("N"));
        Assert.That(transformer.Transform("False", null), Is.EqualTo("N"));
        Assert.That(transformer.Transform("FALSE", null), Is.EqualTo("N"));
    }

    [Test]
    public void BooleanToYNTransformer_HandlesInvalidInput()
    {
        var transformer = new BooleanToYNTransformer();
        Assert.That(transformer.Transform(null, null), Is.EqualTo(string.Empty));
        Assert.That(transformer.Transform("", null), Is.EqualTo(string.Empty));
        Assert.That(transformer.Transform("invalid", null), Is.EqualTo(string.Empty));
        Assert.That(transformer.Transform("1", null), Is.EqualTo(string.Empty));
        Assert.That(transformer.Transform("0", null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void BooleanToYNTransformer_IgnoresArguments()
    {
        var transformer = new BooleanToYNTransformer();
        var args = new Dictionary<string, string?> { ["key"] = "value" };
        Assert.That(transformer.Transform("true", args), Is.EqualTo("Y"));
        Assert.That(transformer.Transform("false", args), Is.EqualTo("N"));
    }
}
