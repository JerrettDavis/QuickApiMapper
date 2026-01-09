using Microsoft.AspNetCore.Mvc;
using QuickApiMapper.Management.Contracts.Models;

namespace QuickApiMapper.Management.Api.Controllers;

/// <summary>
/// API controller for listing available transformers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransformersController : ControllerBase
{
    private readonly ILogger<TransformersController> _logger;

    public TransformersController(ILogger<TransformersController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get list of all available transformers with metadata.
    /// </summary>
    /// <returns>List of transformer metadata.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TransformerMetadata>> GetAll()
    {
        // TODO: Implement dynamic discovery of transformers via reflection
        // For now, return a hardcoded list of common transformers

        var transformers = new List<TransformerMetadata>
        {
            new TransformerMetadata
            {
                Name = "ToUpperCase",
                Description = "Converts the input string to uppercase",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>()
            },
            new TransformerMetadata
            {
                Name = "ToLowerCase",
                Description = "Converts the input string to lowercase",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>()
            },
            new TransformerMetadata
            {
                Name = "Trim",
                Description = "Removes leading and trailing whitespace",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>()
            },
            new TransformerMetadata
            {
                Name = "Substring",
                Description = "Extracts a substring from the input",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "start",
                        Type = "int",
                        Description = "Starting index",
                        IsRequired = true
                    },
                    new TransformerParameterMetadata
                    {
                        Name = "length",
                        Type = "int",
                        Description = "Length of substring",
                        IsRequired = false
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "Replace",
                Description = "Replaces occurrences of a string with another string",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "oldValue",
                        Type = "string",
                        Description = "Value to replace",
                        IsRequired = true
                    },
                    new TransformerParameterMetadata
                    {
                        Name = "newValue",
                        Type = "string",
                        Description = "Replacement value",
                        IsRequired = true
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "FormatDate",
                Description = "Formats a date/time value",
                Category = "DateTime",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "format",
                        Type = "string",
                        Description = "Date format string (e.g., 'yyyy-MM-dd')",
                        IsRequired = true
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "Add",
                Description = "Adds two numeric values",
                Category = "Math",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "value",
                        Type = "decimal",
                        Description = "Value to add",
                        IsRequired = true
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "Multiply",
                Description = "Multiplies two numeric values",
                Category = "Math",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "factor",
                        Type = "decimal",
                        Description = "Multiplication factor",
                        IsRequired = true
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "Concat",
                Description = "Concatenates multiple values",
                Category = "String",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "separator",
                        Type = "string",
                        Description = "Separator between values",
                        IsRequired = false,
                        DefaultValue = ""
                    }
                }
            },
            new TransformerMetadata
            {
                Name = "Default",
                Description = "Provides a default value if input is null or empty",
                Category = "Utility",
                Parameters = new List<TransformerParameterMetadata>
                {
                    new TransformerParameterMetadata
                    {
                        Name = "defaultValue",
                        Type = "string",
                        Description = "Default value to use",
                        IsRequired = true
                    }
                }
            }
        };

        return Ok(transformers);
    }

    /// <summary>
    /// Get transformer by name.
    /// </summary>
    /// <param name="name">Transformer name.</param>
    /// <returns>Transformer metadata if found.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TransformerMetadata> GetByName(string name)
    {
        var transformers = (GetAll().Value as IEnumerable<TransformerMetadata>)?.ToList();
        var transformer = transformers?.FirstOrDefault(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (transformer == null)
        {
            return NotFound(new { message = $"Transformer '{name}' not found" });
        }

        return Ok(transformer);
    }
}

/// <summary>
/// API controller for listing available behaviors.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BehaviorsController : ControllerBase
{
    private readonly ILogger<BehaviorsController> _logger;

    public BehaviorsController(ILogger<BehaviorsController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get list of all available behaviors with metadata.
    /// </summary>
    /// <returns>List of behavior metadata.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BehaviorMetadata>> GetAll()
    {
        // TODO: Implement dynamic discovery of behaviors via reflection
        // For now, return a hardcoded list

        var behaviors = new List<BehaviorMetadata>
        {
            new BehaviorMetadata
            {
                Name = "LoggingBehavior",
                Description = "Logs requests and responses for debugging",
                Category = "Diagnostics",
                ExecutionOrder = 1
            },
            new BehaviorMetadata
            {
                Name = "ValidationBehavior",
                Description = "Validates input data before processing",
                Category = "Validation",
                ExecutionOrder = 2
            },
            new BehaviorMetadata
            {
                Name = "CachingBehavior",
                Description = "Caches responses to improve performance",
                Category = "Performance",
                ExecutionOrder = 3
            },
            new BehaviorMetadata
            {
                Name = "RetryBehavior",
                Description = "Retries failed operations with exponential backoff",
                Category = "Resilience",
                ExecutionOrder = 4
            },
            new BehaviorMetadata
            {
                Name = "CircuitBreakerBehavior",
                Description = "Prevents cascading failures by breaking the circuit",
                Category = "Resilience",
                ExecutionOrder = 5
            }
        };

        return Ok(behaviors);
    }

    /// <summary>
    /// Get behavior by name.
    /// </summary>
    /// <param name="name">Behavior name.</param>
    /// <returns>Behavior metadata if found.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BehaviorMetadata> GetByName(string name)
    {
        var behaviors = (GetAll().Value as IEnumerable<BehaviorMetadata>)?.ToList();
        var behavior = behaviors?.FirstOrDefault(b =>
            b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (behavior == null)
        {
            return NotFound(new { message = $"Behavior '{name}' not found" });
        }

        return Ok(behavior);
    }
}
