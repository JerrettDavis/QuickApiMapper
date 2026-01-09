using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Destinations;

/// <summary>
/// Handles SOAP destination requests by wrapping XML payloads in SOAP envelopes and forwarding them to SOAP services.
/// Supports both configured SOAP envelope construction and fallback envelope generation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the SoapDestinationHandler class.
/// </remarks>
/// <param name="logger">Optional logger for diagnostic information.</param>
public sealed class SoapDestinationHandler(
    ILogger<SoapDestinationHandler>? logger = null) : IDestinationHandler
{
    private readonly ILogger<SoapDestinationHandler>? _logger = logger;

    /// <summary>
    /// Determines if this handler can process the specified destination type.
    /// </summary>
    /// <param name="destinationType">The destination type to check.</param>
    /// <returns>True if the destination type is "SOAP" or "XML" (case-insensitive).</returns>
    public bool CanHandle(string destinationType) =>
        destinationType.Equals("SOAP", StringComparison.OrdinalIgnoreCase) ||
        destinationType.Equals("XML", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Handles a SOAP destination request by wrapping the XML payload in a SOAP envelope and forwarding it.
    /// </summary>
    /// <param name="integration">The integration mapping configuration.</param>
    /// <param name="outJson">The JSON payload (not used by this handler).</param>
    /// <param name="outXml">The XML payload to wrap in SOAP envelope.</param>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="resp">The HTTP response to write to.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public async Task HandleAsync(
        IntegrationMapping integration,
        JObject? outJson,
        XDocument? outXml,
        HttpRequest req,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(resp);

        // Validate that we have XML output
        if (outXml?.Root == null)
        {
            _logger?.LogWarning("No XML output provided for SOAP integration {Integration}", integration.Name);
            resp.StatusCode = 400;
            await resp.WriteAsync("No XML output provided", cancellationToken);
            return;
        }

        try
        {
            _logger?.LogInformation("Building SOAP envelope for integration {Integration}", integration.Name);

            // Get merged static values for SOAP envelope construction
            var staticValues = GetMergedStaticValues(integration, req);

            // Build SOAP envelope
            var soapEnvelope = BuildSoapEnvelope(integration, outXml, staticValues);

            _logger?.LogDebug("Generated SOAP envelope: {Envelope}", soapEnvelope.ToString());

            // Send SOAP request
            await SendSoapRequest(integration, soapEnvelope, resp, httpClientFactory, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to handle SOAP destination for integration {Integration}", integration.Name);
            resp.StatusCode = 500;
            await resp.WriteAsync($"SOAP destination handler failed: {ex.Message}", cancellationToken);
        }
    }

    /// <summary>
    /// Gets merged static values from global and integration-specific sources.
    /// </summary>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="req">The HTTP request for accessing service provider.</param>
    /// <returns>A dictionary of merged static values.</returns>
    private static IReadOnlyDictionary<string, string> GetMergedStaticValues(
        IntegrationMapping integration,
        HttpRequest req)
    {
        // Get global statics from configuration
        var globalStatics = req.HttpContext.RequestServices.GetService(typeof(ApiMappingConfig)) is ApiMappingConfig config
            ? config.StaticValues ?? NewDictionary()
            : NewDictionary();

        // Merge global and integration-specific static values
        return globalStatics
            .Concat(integration.StaticValues ?? NewDictionary())
            .GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);

        Dictionary<string, string> NewDictionary()
            => new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds a SOAP envelope from the integration configuration and XML payload.
    /// </summary>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="outXml">The XML payload to wrap.</param>
    /// <param name="staticValues">The static values for envelope construction.</param>
    /// <returns>A SOAP envelope as an XDocument.</returns>
    private XDocument BuildSoapEnvelope(
        IntegrationMapping integration,
        XDocument outXml,
        IReadOnlyDictionary<string, string> staticValues)
    {
        var soapConfig = integration.SoapConfig;

        // Use fallback envelope if no SOAP config is provided
        if (soapConfig == null)
        {
            _logger?.LogDebug("Using fallback SOAP envelope for integration {Integration}", integration.Name);
            return BuildFallbackSoapEnvelope(outXml, staticValues);
        }

        // Build configured SOAP envelope
        _logger?.LogDebug("Building configured SOAP envelope for integration {Integration}", integration.Name);
        return BuildConfiguredSoapEnvelope(soapConfig, outXml, staticValues);
    }

    /// <summary>
    /// Builds a fallback SOAP envelope when no configuration is provided.
    /// </summary>
    /// <param name="outXml">The XML payload to wrap.</param>
    /// <param name="staticValues">The static values for envelope construction.</param>
    /// <returns>A simple SOAP envelope wrapping the XML payload.</returns>
    private XDocument BuildFallbackSoapEnvelope(
        XDocument outXml,
        IReadOnlyDictionary<string, string> staticValues)
    {
        var soapNs = staticValues.GetValueOrDefault("SoapNamespace", "http://schemas.xmlsoap.org/soap/envelope/");
        XNamespace soap = soapNs;

        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soapNs));

        var body = new XElement(soap + "Body");
        if (outXml.Root != null)
        {
            body.Add(new XElement(outXml.Root));
        }

        envelope.Add(body);
        return new XDocument(envelope);
    }

    /// <summary>
    /// Builds a configured SOAP envelope using the SOAP configuration.
    /// </summary>
    /// <param name="soapConfig">The SOAP configuration.</param>
    /// <param name="outXml">The XML payload to wrap.</param>
    /// <param name="staticValues">The static values for envelope construction.</param>
    /// <returns>A configured SOAP envelope.</returns>
    private XDocument BuildConfiguredSoapEnvelope(
        SoapConfig soapConfig,
        XDocument outXml,
        IReadOnlyDictionary<string, string> staticValues)
    {
        var soapNs = staticValues.GetValueOrDefault("SoapNamespace", "http://schemas.xmlsoap.org/soap/envelope/");
        var tnsNs = staticValues.GetValueOrDefault("TnsNamespace", "urn:services.com:webServices.WebService/v1.0");

        XNamespace soap = soapNs;
        XNamespace tns = tnsNs;

        var envelope = new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soapNs));

        // Build header if configured
        if (soapConfig.HeaderFields?.Count > 0)
        {
            var header = new XElement(soap + "Header");
            BuildSoapFields(header, soapConfig.HeaderFields, staticValues, tns);
            envelope.Add(header);
        }

        // Build body
        var body = new XElement(soap + "Body");
        if (soapConfig.BodyFields?.Count > 0)
        {
            BuildSoapFields(body, soapConfig.BodyFields, staticValues, tns);

            // Add the mapped XML to the wrapper field if specified
            if (!string.IsNullOrEmpty(soapConfig.BodyWrapperFieldXPath))
            {
                var wrapperField = soapConfig.BodyFields.FirstOrDefault(f => f.XPath == soapConfig.BodyWrapperFieldXPath);
                if (wrapperField != null && outXml.Root != null)
                {
                    var wrapperElement = FindElementByXPath(body, wrapperField.XPath);
                    if (wrapperElement != null)
                    {
                        // Create a new element with the wrapper's namespace to avoid blank namespace
                        var namespacedRoot = new XElement(wrapperElement.Name.Namespace + outXml.Root.Name.LocalName);
                        
                        // Copy all attributes and content from the original root
                        foreach (var attr in outXml.Root.Attributes())
                        {
                            namespacedRoot.SetAttributeValue(attr.Name, attr.Value);
                        }
                        
                        // Copy all child elements and content
                        foreach (var node in outXml.Root.Nodes())
                        {
                            namespacedRoot.Add(node);
                        }
                        
                        wrapperElement.Add(namespacedRoot);
                    }
                }
            }
        }
        else if (outXml.Root != null)
        {
            body.Add(outXml.Root);
        }

        envelope.Add(body);
        return new XDocument(envelope);
    }

    /// <summary>
    /// Builds SOAP fields from configuration.
    /// </summary>
    /// <param name="parent">The parent element to add fields to.</param>
    /// <param name="fields">The field configurations.</param>
    /// <param name="staticValues">The static values for field population.</param>
    /// <param name="defaultNamespace">The default namespace for elements.</param>
    private void BuildSoapFields(
        XElement parent,
        IReadOnlyList<SoapFieldConfig> fields,
        IReadOnlyDictionary<string, string> staticValues,
        XNamespace defaultNamespace)
    {
        foreach (var field in fields)
        {
            var element = CreateSoapElement(parent, field, defaultNamespace);

            // Set value if source is provided
            if (!string.IsNullOrEmpty(field.Source))
            {
                var value = ResolveStaticValue(field.Source, staticValues);
                if (value != null)
                {
                    element.Value = value;
                }
            }

            // Set attributes if configured
            if (field.Attributes != null)
            {
                foreach (var attr in field.Attributes)
                {
                    element.SetAttributeValue(attr.Key, attr.Value);
                }
            }
        }
    }

    /// <summary>
    /// Creates a SOAP element from field configuration.
    /// </summary>
    /// <param name="parent">The parent element.</param>
    /// <param name="field">The field configuration.</param>
    /// <param name="defaultNamespace">The default namespace.</param>
    /// <returns>The created element.</returns>
    private static XElement CreateSoapElement(
        XElement parent,
        SoapFieldConfig field,
        XNamespace defaultNamespace)
    {
        var parts = field.XPath.Split('/');
        var current = parent;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            var ns = !string.IsNullOrEmpty(field.Namespace) ? field.Namespace : defaultNamespace.NamespaceName;
            XNamespace xmlns = ns;

            var existing = current.Elements().FirstOrDefault(e => e.Name.LocalName == part);
            if (existing == null)
            {
                existing = new XElement(xmlns + part);
                current.Add(existing);
            }

            current = existing;
        }

        return current;
    }

    /// <summary>
    /// Finds an element by XPath (simple implementation).
    /// </summary>
    /// <param name="parent">The parent element to search in.</param>
    /// <param name="xpath">The XPath to search for.</param>
    /// <returns>The found element or null.</returns>
    private static XElement? FindElementByXPath(XElement parent, string xpath)
    {
        var parts = xpath.Split('/');
        var current = parent;

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            current = current.Elements().FirstOrDefault(e => e.Name.LocalName == part);
            if (current == null) return null;
        }

        return current;
    }

    /// <summary>
    /// Resolves a static value from the source path.
    /// </summary>
    /// <param name="source">The source path.</param>
    /// <param name="staticValues">The static values dictionary.</param>
    /// <returns>The resolved value or the source as literal.</returns>
    private static string? ResolveStaticValue(string source, IReadOnlyDictionary<string, string> staticValues)
    {
        if (source.StartsWith("$$."))
        {
            var key = source[3..];
            return staticValues.TryGetValue(key, out var value) ? value : null;
        }

        return source; // Return as literal value if not a static reference
    }

    /// <summary>
    /// Sends the SOAP request to the destination URL.
    /// </summary>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="soapEnvelope">The SOAP envelope to send.</param>
    /// <param name="resp">The HTTP response to write to.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task SendSoapRequest(
        IntegrationMapping integration,
        XDocument soapEnvelope,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogInformation("Sending SOAP request to {Url} for integration {Integration}", 
                integration.DestinationUrl, integration.Name);

            using var client = httpClientFactory.CreateClient();
            var envelopeContent = soapEnvelope.ToString();
            var content = new StringContent(envelopeContent, Encoding.UTF8, "text/xml");

            using var request = new HttpRequestMessage(HttpMethod.Post, integration.DestinationUrl);
            request.Content = content;

            _logger?.LogDebug("Outgoing SOAP envelope: {Envelope}", envelopeContent);

            using var httpResponse = await client.SendAsync(request, cancellationToken);

            resp.StatusCode = (int)httpResponse.StatusCode;
            resp.ContentType = httpResponse.Content.Headers.ContentType?.ToString() ?? "text/xml";

            var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            await resp.WriteAsync(responseBody, cancellationToken);

            _logger?.LogInformation("SOAP request completed with status {StatusCode} for integration {Integration}", 
                httpResponse.StatusCode, integration.Name);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "SOAP HTTP request failed for integration {Integration} to {Url}", 
                integration.Name, integration.DestinationUrl);
            resp.StatusCode = 502;
            await resp.WriteAsync($"Failed to communicate with SOAP service: {ex.Message}", cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger?.LogError(ex, "SOAP request timeout for integration {Integration} to {Url}", 
                integration.Name, integration.DestinationUrl);
            resp.StatusCode = 504;
            await resp.WriteAsync("SOAP request timed out.", cancellationToken);
        }
    }
}
