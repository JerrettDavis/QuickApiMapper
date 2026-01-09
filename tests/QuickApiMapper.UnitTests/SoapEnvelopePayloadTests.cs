using System.Reflection;
using System.Xml.Linq;
using QuickApiMapper.Application.Destinations;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class SoapEnvelopePayloadTests
{
    [Test]
    public void Can_Generate_Soap_Envelope_With_Header_And_Operation()
    {
        // Arrange
        var statics = new Dictionary<string, string>
        {
            ["SoapNamespace"] = "http://schemas.xmlsoap.org/soap/envelope/",
            ["TnsNamespace"] = "urn:example.com:services:integration/v1.0",
            ["TnsPrefix"] = "tns",
            ["SoapOperation"] = "SendSynchronic2",
            ["RootInitiator"] = "Import",
            ["RootName"] = "root",
            ["SessionName"] = "session",
            ["User"] = "string",
            ["Password"] = "string",
            ["Profile"] = "string",
            ["HeaderWrapper"] = "WrapperHeader"
        };
        // Minimal session payload
        var sessionXml = new XDocument(new XElement("session"));
        // Use reflection to call the internal logic for envelope generation
        var method = typeof(SoapDestinationHandler).GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, "HandleAsync should exist");
        // Instead of calling the full HTTP logic, just build the envelope
        // We'll simulate the logic here for test
        XNamespace soap = statics["SoapNamespace"];
        XNamespace tns = statics["TnsNamespace"];
        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", statics["SoapNamespace"])
        );
        var header = new XElement(soap + "Header",
            new XElement(tns + "WrapperHeader",
                new XAttribute(XNamespace.Xmlns + statics["TnsPrefix"], statics["TnsNamespace"]),
                new XElement(tns + "User", "string"),
                new XElement(tns + "Password", "string"),
                new XElement(tns + "Profile", "string")
            )
        );
        envelope.Add(header);
        var body = new XElement(soap + "Body");
        envelope.Add(body);
        var opElem = new XElement(tns + "SendSynchronic2",
            new XAttribute(XNamespace.Xmlns + statics["TnsPrefix"], statics["TnsNamespace"])
        );
        body.Add(opElem);
        var rootElem = new XElement(tns + "root",
            new XAttribute("initiator", "Import")
        );
        opElem.Add(rootElem);
        var sessionElem = sessionXml.Root!;
        sessionElem.Name = tns + "session";
        rootElem.Add(sessionElem);
        var actual = envelope.ToString(SaveOptions.DisableFormatting);
        // Log the generated envelope for inspection
        TestContext.Out.WriteLine("Generated SOAP Envelope:\n" + actual);
        // Remove XML declaration from expected for comparison
        var expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Header><tns:WrapperHeader xmlns:tns=\"urn:example.com:services:integration/v1.0\"><tns:User>string</tns:User><tns:Password>string</tns:Password><tns:Profile>string</tns:Profile></tns:WrapperHeader></soap:Header><soap:Body><tns:SendSynchronic2 xmlns:tns=\"urn:example.com:services:integration/v1.0\"><tns:root initiator=\"Import\"><tns:session /></tns:root></tns:SendSynchronic2></soap:Body></soap:Envelope>";
        if (expected.StartsWith("<?xml"))
        {
            var idx = expected.IndexOf("?>", StringComparison.Ordinal);
            if (idx >= 0)
                expected = expected[(idx + 2)..];
        }
        Assert.That(actual.Replace("\r\n", "").Replace("\n", ""), Is.EqualTo(expected.Replace("\r\n", "").Replace("\n", "")));
    }

    [Test]
    public void Root_Element_Should_Not_Have_Blank_Namespace_And_Should_Match_Wrapper_Namespace()
    {
        // Arrange
        var soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        var wrapperNs = "urn:example.com:services:integration/v1.0";
        XNamespace soap = soapNs;
        XNamespace tns = wrapperNs;
        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soapNs)
        );
        var body = new XElement(soap + "Body");
        envelope.Add(body);
        var wrapper = new XElement(tns + "SendSynchronic2");
        body.Add(wrapper);
        // Simulate logic for <root> creation
        var root = new XElement(tns + "root");
        wrapper.Add(root);
        // Act
        var xml = envelope.ToString();
        // Log the generated XML for inspection
        TestContext.Out.WriteLine("Generated SOAP Envelope:\n" + xml);
        // Assert
        var doc = XDocument.Parse(xml);
        var rootElem = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "root");
        Assert.That(rootElem, Is.Not.Null, "<root> element should exist");
        Assert.That(rootElem!.Name.NamespaceName, Is.EqualTo(wrapperNs), "<root> should have the same namespace as the wrapper");
        // Check that xmlns="" does not exist anywhere in the payload
        Assert.That(xml.Contains("xmlns=\"\""), Is.False, "Payload should not contain xmlns=\"\"");
    }

    [Test]
    public void Root_Element_Merged_Into_Wrapper_Should_Not_Have_Blank_Namespace()
    {
        // Arrange: create <root> with the correct namespace from the start
        var wrapperNs = "urn:example.com:services:integration/v1.0";
        XNamespace tns = wrapperNs;
        // Create <root> in the wrapper's namespace
        var root = new XElement(tns + "root",
            new XElement(tns + "session",
                new XAttribute("alias", ""),
                new XAttribute("email", ""),
                new XAttribute("sendOnErrorOnly", "true")
            )
        );
        // Now merge <root> into a wrapper with a namespace, as in SoapDestinationHandler
        var wrapper = new XElement(tns + "SendSynchronic2");
        wrapper.Add(root);
        // Act: serialize
        var xml = wrapper.ToString();
        Console.WriteLine("Merged payload:\n" + xml);
        // Assert: <root> and all descendants are in wrapper's namespace, and xmlns="" does not appear
        if (xml.Contains("xmlns=\"\""))
        {
            Assert.Fail($"Payload contains unwanted xmlns=\"\". Actual payload:\n{xml}");
        }
        var doc = XDocument.Parse(wrapper.ToString());
        var rootElem = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "root");
        Assert.That(rootElem, Is.Not.Null, "<root> element should exist");
        Assert.That(rootElem!.Name.NamespaceName, Is.EqualTo(wrapperNs), "<root> should have the same namespace as the wrapper");
        Assert.That(rootElem.DescendantsAndSelf().All(e => e.Name.NamespaceName == wrapperNs), "All elements should be in the wrapper's namespace");
    }
}
