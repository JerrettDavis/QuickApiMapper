using Microsoft.AspNetCore.Mvc;
using QuickApiMapper.Management.Api.Models;
using QuickApiMapper.Management.Api.Services;

namespace QuickApiMapper.Management.Api.Controllers;

/// <summary>
/// API controller for managing integration mappings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly ITestingService _testingService;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        IIntegrationService integrationService,
        ITestingService testingService,
        ILogger<IntegrationsController> logger)
    {
        _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
        _testingService = testingService ?? throw new ArgumentNullException(nameof(testingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all integrations.
    /// </summary>
    /// <returns>List of all integrations.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IntegrationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var integrations = await _integrationService.GetAllAsync(cancellationToken);
        return Ok(integrations);
    }

    /// <summary>
    /// Get integration by ID.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integration if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var integration = await _integrationService.GetByIdAsync(id, cancellationToken);
        if (integration == null)
        {
            return NotFound(new { message = $"Integration with ID {id} not found" });
        }

        return Ok(integration);
    }

    /// <summary>
    /// Get integration by name.
    /// </summary>
    /// <param name="name">Integration name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integration if found.</returns>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> GetByName(string name, CancellationToken cancellationToken)
    {
        var integration = await _integrationService.GetByNameAsync(name, cancellationToken);
        if (integration == null)
        {
            return NotFound(new { message = $"Integration with name '{name}' not found" });
        }

        return Ok(integration);
    }

    /// <summary>
    /// Get integration by endpoint.
    /// </summary>
    /// <param name="endpoint">Integration endpoint path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integration if found.</returns>
    [HttpGet("by-endpoint")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> GetByEndpoint([FromQuery] string endpoint, CancellationToken cancellationToken)
    {
        var integration = await _integrationService.GetByEndpointAsync(endpoint, cancellationToken);
        if (integration == null)
        {
            return NotFound(new { message = $"Integration with endpoint '{endpoint}' not found" });
        }

        return Ok(integration);
    }

    /// <summary>
    /// Create a new integration.
    /// </summary>
    /// <param name="request">Integration creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created integration.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IntegrationDto>> Create(
        [FromBody] CreateIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var integration = await _integrationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = integration.Id },
                integration);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create integration: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="request">Integration update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated integration.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> Update(
        Guid id,
        [FromBody] UpdateIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var integration = await _integrationService.UpdateAsync(id, request, cancellationToken);
            return Ok(integration);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Integration not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update integration: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an integration.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _integrationService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = $"Integration with ID {id} not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Test an integration with sample data.
    /// </summary>
    /// <param name="id">Integration ID.</param>
    /// <param name="request">Test request with sample payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test result with transformed payload.</returns>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TestMappingResponse>> Test(
        Guid id,
        [FromBody] TestMappingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _testingService.ExecuteTestAsync(id, request, cancellationToken);

        if (!result.Success && result.Errors?.Contains("not found") == true)
        {
            return NotFound(new { message = result.Errors });
        }

        return Ok(result);
    }
}
