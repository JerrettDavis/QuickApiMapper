using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Behaviors;

/// <summary>
/// Timing behavior that measures and logs execution time for mapping operations.
/// Implements WholeRun behavior to wrap the entire mapping process.
/// </summary>
public sealed class TimingBehavior(ILogger<TimingBehavior> logger) : IWholeRunBehavior
{

    public string Name => "Timing";
    public int Order => 10; // Execute as outer wrapper

    public async Task<MappingResult> ExecuteAsync(MappingContext context, Func<MappingContext, Task<MappingResult>> next)
    {
        logger.LogDebug("Starting timing behavior for {MappingCount} mappings", context.Mappings.Count());
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await next(context);
            
            stopwatch.Stop();
            
            // Store timing information in the result
            result.Properties["ExecutionTime"] = stopwatch.Elapsed;
            
            logger.LogInformation("Mapping execution completed in {ElapsedMs}ms. Success: {Success}", 
                stopwatch.ElapsedMilliseconds, result.IsSuccess);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            logger.LogError(ex, "Mapping execution failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            var failureResult = MappingResult.Failure("Mapping execution failed", ex);
            failureResult.Properties["ExecutionTime"] = stopwatch.Elapsed;
            
            return failureResult;
        }
    }
}