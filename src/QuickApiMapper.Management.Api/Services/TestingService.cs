using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Extensions;
using QuickApiMapper.Contracts;
using QuickApiMapper.Management.Contracts.Models;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Management.Api.Services;

/// <summary>
/// Service for testing integration mappings without calling destination systems.
/// </summary>
public class TestingService : ITestingService
{
    private readonly IIntegrationMappingRepository _repository;
    private readonly IMappingEngineFactory _mappingEngineFactory;
    private readonly IMessageCaptureProvider _messageCaptureProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TestingService> _logger;

    public TestingService(
        IIntegrationMappingRepository repository,
        IMappingEngineFactory mappingEngineFactory,
        IMessageCaptureProvider messageCaptureProvider,
        IServiceProvider serviceProvider,
        ILogger<TestingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mappingEngineFactory = mappingEngineFactory ?? throw new ArgumentNullException(nameof(mappingEngineFactory));
        _messageCaptureProvider = messageCaptureProvider ?? throw new ArgumentNullException(nameof(messageCaptureProvider));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TestMappingResponse> ExecuteTestAsync(
        Guid integrationId,
        TestMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Testing integration {IntegrationId} with correlation {CorrelationId}", integrationId, correlationId);

            // Load integration configuration
            var entity = await _repository.GetByIdAsync(integrationId, cancellationToken);
            if (entity == null)
            {
                return new TestMappingResponse
                {
                    Success = false,
                    Errors = $"Integration with ID {integrationId} not found"
                };
            }

            if (string.IsNullOrWhiteSpace(request.SamplePayload))
            {
                return new TestMappingResponse
                {
                    Success = false,
                    Errors = "Sample payload is required"
                };
            }

            // Capture input message
            await _messageCaptureProvider.CaptureAsync(new CapturedMessage
            {
                Id = $"{correlationId}-input",
                IntegrationId = integrationId,
                IntegrationName = entity.Name,
                Direction = MessageDirection.Input,
                Payload = request.SamplePayload,
                Status = MessageStatus.Pending,
                CorrelationId = correlationId,
                Timestamp = startTime,
                Metadata = new Dictionary<string, string>
                {
                    { "Source", "DemoRunner" },
                    { "TestMode", "true" }
                }
            }, cancellationToken);

            // Convert entity to integration mapping for processing
            var integration = ConvertToIntegrationMapping(entity);

            // Merge static values with any overrides
            var staticValueDict = new Dictionary<string, string>(integration.StaticValues ?? new Dictionary<string, string>());
            if (request.OverrideStaticValues != null)
            {
                foreach (var kvp in request.OverrideStaticValues)
                {
                    staticValueDict[kvp.Key] = kvp.Value;
                }
            }
            IReadOnlyDictionary<string, string> staticValues = staticValueDict;

            // Process based on source and destination types
            var sourceType = entity.SourceType.ToUpperInvariant();
            var destinationType = entity.DestinationType.ToUpperInvariant();

            string? transformedPayload = null;

            if (sourceType == "JSON")
            {
                var inputJson = JObject.Parse(request.SamplePayload);

                if (destinationType == "JSON")
                {
                    transformedPayload = await ProcessJsonToJson(integration, inputJson, staticValues, cancellationToken);
                }
                else if (destinationType is "XML" or "SOAP")
                {
                    transformedPayload = await ProcessJsonToXml(integration, inputJson, staticValues, cancellationToken);
                }
            }
            else if (sourceType is "XML" or "SOAP")
            {
                var inputXml = XDocument.Parse(request.SamplePayload);

                if (destinationType == "JSON")
                {
                    transformedPayload = await ProcessXmlToJson(integration, inputXml, staticValues, cancellationToken);
                }
                else if (destinationType is "XML" or "SOAP")
                {
                    transformedPayload = await ProcessXmlToXml(integration, inputXml, staticValues, cancellationToken);
                }
            }
            else
            {
                return new TestMappingResponse
                {
                    Success = false,
                    Errors = $"Unsupported source type: {entity.SourceType}"
                };
            }

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Capture output message
            await _messageCaptureProvider.CaptureAsync(new CapturedMessage
            {
                Id = $"{correlationId}-output",
                IntegrationId = integrationId,
                IntegrationName = entity.Name,
                Direction = MessageDirection.Output,
                Payload = transformedPayload ?? string.Empty,
                Status = MessageStatus.Success,
                CorrelationId = correlationId,
                Timestamp = endTime,
                Duration = duration,
                Metadata = new Dictionary<string, string>
                {
                    { "Source", "DemoRunner" },
                    { "TestMode", "true" },
                    { "FieldMappingCount", (integration.Mapping?.Count ?? 0).ToString() }
                }
            }, cancellationToken);

            _logger.LogInformation("Successfully tested integration {IntegrationId}, correlation {CorrelationId}", integrationId, correlationId);

            return new TestMappingResponse
            {
                Success = true,
                TransformedPayload = transformedPayload,
                Metadata = new Dictionary<string, string>
                {
                    { "SourceType", entity.SourceType },
                    { "DestinationType", entity.DestinationType },
                    { "IntegrationName", entity.Name },
                    { "CorrelationId", correlationId },
                    { "Duration", duration.TotalMilliseconds.ToString("F0") }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing integration {IntegrationId}, correlation {CorrelationId}", integrationId, correlationId);

            // Capture failed output message
            var endTime = DateTime.UtcNow;
            await _messageCaptureProvider.CaptureAsync(new CapturedMessage
            {
                Id = $"{correlationId}-output",
                IntegrationId = integrationId,
                IntegrationName = "Unknown",
                Direction = MessageDirection.Output,
                Payload = string.Empty,
                Status = MessageStatus.Failed,
                ErrorMessage = ex.Message,
                CorrelationId = correlationId,
                Timestamp = endTime,
                Duration = endTime - startTime,
                Metadata = new Dictionary<string, string>
                {
                    { "Source", "DemoRunner" },
                    { "TestMode", "true" }
                }
            }, cancellationToken);

            return new TestMappingResponse
            {
                Success = false,
                Errors = $"Error testing integration: {ex.Message}",
                Metadata = new Dictionary<string, string>
                {
                    { "CorrelationId", correlationId }
                }
            };
        }
    }

    private async Task<string> ProcessJsonToJson(
        IntegrationMapping integration,
        JObject inputJson,
        IReadOnlyDictionary<string, string> staticValues,
        CancellationToken cancellationToken)
    {
        var outputJson = new JObject();
        var engine = _mappingEngineFactory.CreateEngine<JObject, JObject>();

        await engine.ApplyMappingAsync(
            integration.Mapping ?? new List<FieldMapping>(),
            inputJson,
            outputJson,
            staticValues,
            null, // Global static values
            _serviceProvider,
            cancellationToken);

        return outputJson.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    private async Task<string> ProcessJsonToXml(
        IntegrationMapping integration,
        JObject inputJson,
        IReadOnlyDictionary<string, string> staticValues,
        CancellationToken cancellationToken)
    {
        var outputXml = CreateXmlDocument(integration, staticValues);
        var engine = _mappingEngineFactory.CreateEngine<JObject, XDocument>();

        await engine.ApplyMappingAsync(
            integration.Mapping ?? new List<FieldMapping>(),
            inputJson,
            outputXml,
            staticValues,
            null,
            _serviceProvider,
            cancellationToken);

        return outputXml.ToString();
    }

    private async Task<string> ProcessXmlToJson(
        IntegrationMapping integration,
        XDocument inputXml,
        IReadOnlyDictionary<string, string> staticValues,
        CancellationToken cancellationToken)
    {
        var outputJson = new JObject();
        var engine = _mappingEngineFactory.CreateEngine<XDocument, JObject>();

        await engine.ApplyMappingAsync(
            integration.Mapping ?? new List<FieldMapping>(),
            inputXml,
            outputJson,
            staticValues,
            null,
            _serviceProvider,
            cancellationToken);

        return outputJson.ToString(Newtonsoft.Json.Formatting.Indented);
    }

    private async Task<string> ProcessXmlToXml(
        IntegrationMapping integration,
        XDocument inputXml,
        IReadOnlyDictionary<string, string> staticValues,
        CancellationToken cancellationToken)
    {
        var outputXml = CreateXmlDocument(integration, staticValues);
        var engine = _mappingEngineFactory.CreateEngine<XDocument, XDocument>();

        await engine.ApplyMappingAsync(
            integration.Mapping ?? new List<FieldMapping>(),
            inputXml,
            outputXml,
            staticValues,
            null,
            _serviceProvider,
            cancellationToken);

        return outputXml.ToString();
    }

    private XDocument CreateXmlDocument(IntegrationMapping integration, IReadOnlyDictionary<string, string> staticValues)
    {
        var rootElementName = "root";
        XNamespace rootNamespace = "";

        // Use TNS namespace from static values if available
        if (staticValues.TryGetValue("TnsNamespace", out var tnsNamespace))
        {
            rootNamespace = tnsNamespace;
        }

        return new XDocument(new XElement(rootNamespace + rootElementName));
    }

    private IntegrationMapping ConvertToIntegrationMapping(Persistence.Abstractions.Models.IntegrationMappingEntity entity)
    {
        var fieldMappings = entity.FieldMappings?.Select(fm => new FieldMapping(
            Source: fm.Source,
            Destination: fm.Destination,
            Transformers: fm.Transformers?.Select(t => new Transformer(
                Name: t.Name,
                Args: string.IsNullOrEmpty(t.Arguments)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(t.Arguments)
            )).ToList()
        )).ToList();

        var staticValues = entity.StaticValues?.ToDictionary(sv => sv.Key, sv => sv.Value);

        return new IntegrationMapping(
            Name: entity.Name,
            Endpoint: entity.Endpoint,
            SourceType: entity.SourceType,
            DestinationType: entity.DestinationType,
            DestinationUrl: entity.DestinationUrl,
            PayloadArguments: null,
            DispatchFor: null,
            StaticValues: staticValues,
            Mapping: fieldMappings,
            SoapHeaderXml: null,
            SoapConfig: null
        );
    }
}

/// <summary>
/// Interface for testing service.
/// </summary>
public interface ITestingService
{
    Task<TestMappingResponse> ExecuteTestAsync(
        Guid integrationId,
        TestMappingRequest request,
        CancellationToken cancellationToken = default);
}
