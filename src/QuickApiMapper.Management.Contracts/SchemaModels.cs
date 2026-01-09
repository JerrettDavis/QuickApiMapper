namespace QuickApiMapper.Management.Contracts.Models;

/// <summary>
/// Represents a node in a schema tree (JSON, XML, gRPC).
/// Used for visualizing schema structure in the designer.
/// </summary>
public class SchemaTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsArray { get; set; }
    public bool IsRequired { get; set; }
    public List<SchemaTreeNode>? Children { get; set; }
}

/// <summary>
/// Request for importing a JSON schema.
/// </summary>
public class ImportJsonSchemaRequest
{
    public string? SchemaUrl { get; set; }
    public string? SchemaContent { get; set; }
}

/// <summary>
/// Request for importing a gRPC proto file.
/// </summary>
public class ImportProtoFileRequest
{
    public string ProtoFileName { get; set; } = string.Empty;
    public string ProtoFileContent { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
}

/// <summary>
/// Request for importing a WSDL file.
/// </summary>
public class ImportWsdlRequest
{
    public string WsdlUrl { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? PortName { get; set; }
    public string? OperationName { get; set; }
}

/// <summary>
/// Response from schema import operations.
/// </summary>
public class SchemaImportResponse
{
    public bool Success { get; set; }
    public SchemaTreeNode? SchemaTree { get; set; }
    public List<string>? Errors { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Transformer metadata for listing available transformers.
/// </summary>
public class TransformerMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<TransformerParameterMetadata>? Parameters { get; set; }
}

/// <summary>
/// Transformer parameter metadata.
/// </summary>
public class TransformerParameterMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Behavior metadata for listing available behaviors.
/// </summary>
public class BehaviorMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ExecutionOrder { get; set; }
}

/// <summary>
/// Represents a single step in the transformation process.
/// </summary>
public class TransformationStep
{
    public string FieldPath { get; set; } = string.Empty;
    public string? SourceValue { get; set; }
    public string? TransformedValue { get; set; }
    public List<string>? TransformersApplied { get; set; }
}
