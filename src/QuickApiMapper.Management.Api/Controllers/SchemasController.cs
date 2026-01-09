using Microsoft.AspNetCore.Mvc;
using QuickApiMapper.Management.Api.Models;
using QuickApiMapper.Management.Api.Services;

namespace QuickApiMapper.Management.Api.Controllers;

/// <summary>
/// API controller for schema import and validation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SchemasController : ControllerBase
{
    private readonly ISchemaImportService _schemaImportService;
    private readonly ILogger<SchemasController> _logger;

    public SchemasController(
        ISchemaImportService schemaImportService,
        ILogger<SchemasController> logger)
    {
        _schemaImportService = schemaImportService ?? throw new ArgumentNullException(nameof(schemaImportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Import a JSON schema and return tree structure.
    /// </summary>
    /// <param name="request">JSON schema import request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Schema tree structure.</returns>
    [HttpPost("json/import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SchemaImportResponse>> ImportJsonSchema(
        [FromBody] ImportJsonSchemaRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _schemaImportService.ImportJsonSchemaAsync(request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Import a gRPC proto file and return message structure.
    /// </summary>
    /// <param name="request">Proto file import request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Schema tree structure.</returns>
    [HttpPost("grpc/import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SchemaImportResponse>> ImportProtoFile(
        [FromBody] ImportProtoFileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _schemaImportService.ImportProtoFileAsync(request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Import a WSDL and return operation structure.
    /// </summary>
    /// <param name="request">WSDL import request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Schema tree structure.</returns>
    [HttpPost("wsdl/import")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SchemaImportResponse>> ImportWsdl(
        [FromBody] ImportWsdlRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _schemaImportService.ImportWsdlAsync(request, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Validate an integration configuration.
    /// </summary>
    /// <param name="integration">Integration configuration to validate.</param>
    /// <returns>Validation result.</returns>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> ValidateIntegration([FromBody] IntegrationDto integration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(integration.Name))
            errors.Add("Integration name is required");

        if (string.IsNullOrWhiteSpace(integration.Endpoint))
            errors.Add("Endpoint is required");

        if (string.IsNullOrWhiteSpace(integration.SourceType))
            errors.Add("Source type is required");

        if (string.IsNullOrWhiteSpace(integration.DestinationType))
            errors.Add("Destination type is required");

        if (string.IsNullOrWhiteSpace(integration.DestinationUrl))
            errors.Add("Destination URL is required");

        return Ok(new
        {
            isValid = errors.Count == 0,
            errors = errors.Count > 0 ? errors : null
        });
    }
}
