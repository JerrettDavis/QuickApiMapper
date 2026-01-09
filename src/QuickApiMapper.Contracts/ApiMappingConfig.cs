namespace QuickApiMapper.Contracts;

public record ApiMappingConfig(
    IReadOnlyDictionary<string, string>? Namespaces,
    IReadOnlyList<IntegrationMapping>? Mappings,
    IReadOnlyDictionary<string,string>? StaticValues
);

public record IntegrationMapping(
    string Name,
    string Endpoint,
    string SourceType,          // e.g. "JSON"
    string DestinationType,     // e.g. "SOAP"
    string DestinationUrl,
    IReadOnlyList<PayloadArg>? PayloadArguments,
    string? DispatchFor,
    IReadOnlyDictionary<string, string>? StaticValues,
    IReadOnlyList<FieldMapping>? Mapping,
    string? SoapHeaderXml = null, // Optional: raw XML fragment for SOAP header
    SoapConfig? SoapConfig = null, // New: config-driven SOAP header/body structure
    bool EnableInput = true, // Controls whether integration accepts requests
    bool EnableOutput = true, // Controls whether transformed data is forwarded
    bool EnableMessageCapture = true // Controls whether messages are captured
);

public record PayloadArg(
    string Type,
    string Value
);

// FieldMapping and Transformer are now defined in IBehavior.cs

public record SoapConfig(
    IReadOnlyList<SoapFieldConfig>? HeaderFields,
    IReadOnlyList<SoapFieldConfig>? BodyFields = null, // Optional: for future body config
    string? BodyWrapperFieldXPath = null // NEW: XPath of the body field to use as the wrapper
);

public record SoapFieldConfig(
    string XPath, // e.g. "WrapperHeader/User"
    string Source, // e.g. "$$.Username" or "$.payloadField"
    IReadOnlyList<Transformer>? Transformers = null, // Optional
    string? Namespace = null, // Optional: XML namespace for this element
    string? Prefix = null, // Optional: XML prefix for this element
    IReadOnlyDictionary<string, string>? Attributes = null // Optional: attributes for this element
);
