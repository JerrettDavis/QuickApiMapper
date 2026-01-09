using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Behaviors;

/// <summary>
/// Validation behavior that validates mapping context before execution.
/// </summary>
public sealed class ValidationBehavior(ILogger<ValidationBehavior> logger) : IPreRunBehavior
{

    public string Name => "Validation";
    public int Order => 50; // Execute early in PreRun pipeline

    public Task ExecuteAsync(MappingContext context)
    {
        logger.LogDebug("Starting validation behavior");

        try
        {
            ValidateMappingContext(context);
            logger.LogDebug("Validation behavior completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation behavior failed");
            throw;
        }

        return Task.CompletedTask;
    }

    private void ValidateMappingContext(MappingContext context)
    {
        // Validate that we have at least one mapping
        if (!context.Mappings.Any())
        {
            throw new InvalidOperationException("No mappings provided for execution");
        }

        // Validate that we have at least one data source
        if (context.Source == null && context.Destination == null && 
            (context.Statics == null || !context.Statics.Any()) &&
            (context.GlobalStatics == null || !context.GlobalStatics.Any()))
        {
            throw new InvalidOperationException("No data sources provided (JSON, XML, or static values)");
        }

        // Validate mappings have required properties
        var invalidMappings = context.Mappings.Where(m => string.IsNullOrEmpty(m.Source)).ToList();
        if (invalidMappings.Any())
        {
            throw new InvalidOperationException($"Found {invalidMappings.Count} mappings with empty source paths");
        }

        logger.LogDebug("Validation passed for {MappingCount} mappings", context.Mappings.Count());
    }
}