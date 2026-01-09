using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;
using ContractsMappingResult = QuickApiMapper.Contracts.MappingResult;

namespace QuickApiMapper.Application.Core;

/// <summary>
/// Executes behaviors in a pipeline pattern.
/// </summary>
public sealed class BehaviorPipeline(
    IEnumerable<IPreRunBehavior> preRunBehaviors,
    IEnumerable<IPostRunBehavior> postRunBehaviors,
    IEnumerable<IWholeRunBehavior> wholeRunBehaviors,
    ILogger<BehaviorPipeline> logger
)
{
    /// <summary>
    /// Executes the complete behavior pipeline around the core mapping logic.
    /// </summary>
    /// <param name="context">The mapping context.</param>
    /// <param name="coreLogic">The core mapping logic to execute.</param>
    /// <returns>The mapping result.</returns>
    public async Task<ContractsMappingResult> ExecuteAsync(
        MappingContext context,
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        // Build the complete pipeline: PreRun -> WholeRun -> Core -> PostRun
        var pipeline = BuildCompletePipeline(coreLogic);

        // Execute the complete pipeline (exceptions from PreRun behaviors will propagate)
        var result = await pipeline(context);

        logger.LogInformation("Behavior pipeline execution completed. Success: {Success}", result.IsSuccess);
        return result;
    }

    /// <summary>
    /// Builds the complete pipeline: PreRun -> WholeRun -> Core -> PostRun
    /// </summary>
    private Func<MappingContext, Task<ContractsMappingResult>> BuildCompletePipeline(
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        
        var wholeRunPipeline = BuildWholeRunPipeline(coreLogic);
        var wholeRunWithPost = BuildCoreWithPostRun(wholeRunPipeline);

        return BuildPreRunPipeline(wholeRunWithPost);
    }

    /// <summary>
    /// Builds the PreRun behavior pipeline chain.
    /// </summary>
    private Func<MappingContext, Task<ContractsMappingResult>> BuildPreRunPipeline(
        Func<MappingContext, Task<ContractsMappingResult>> next)
    {
        return async context =>
        {
            // Execute PreRun behaviors first (let exceptions propagate for fail-fast scenarios)
            await ExecutePreRunBehaviors(context);

            // Then execute the rest of the pipeline
            return await next(context);
        };
    }

    /// <summary>
    /// Builds the WholeRun behavior pipeline chain.
    /// </summary>
    private Func<MappingContext, Task<ContractsMappingResult>> BuildWholeRunPipeline(
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        // Get ordered WholeRun behaviors
        var orderedBehaviors = wholeRunBehaviors
            .OrderBy(b => b.Order)
            .ToList();

        // Build the pipeline from right to left (last behavior wraps the core logic)
        var pipeline = coreLogic;

        // Wrap with WholeRun behaviors in reverse order
        for (var i = orderedBehaviors.Count - 1; i >= 0; i--)
        {
            var behavior = orderedBehaviors[i];
            var next = pipeline; // Capture the current pipeline

            pipeline = async context =>
            {
                logger.LogDebug("Executing WholeRun behavior: {BehaviorName}", behavior.Name);
                return await behavior.ExecuteAsync(context, next);
            };
        }

        return pipeline;
    }

    /// <summary>
    /// Builds the core logic wrapped with PostRun behaviors.
    /// </summary>
    private Func<MappingContext, Task<ContractsMappingResult>> BuildCoreWithPostRun(
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        return async context =>
        {
            try
            {
                // Execute core logic
                var result = await coreLogic(context);

                // Execute PostRun behaviors
                await ExecutePostRunBehaviors(context, result);

                return result;
            }
            catch (Exception ex)
            {
                var failureResult = ContractsMappingResult.Failure("Core mapping logic failed", ex);

                // Still try to execute PostRun behaviors even if core logic failed
                try
                {
                    await ExecutePostRunBehaviors(context, failureResult);
                }
                catch (Exception postRunEx)
                {
                    logger.LogError(postRunEx, "PostRun behavior execution failed after core logic failure");
                }

                return failureResult;
            }
        };
    }


    /// <summary>
    /// Executes all PreRun behaviors in order.
    /// </summary>
    private async Task ExecutePreRunBehaviors(MappingContext context)
    {
        var orderedBehaviors = preRunBehaviors
            .OrderBy(b => b.Order)
            .ToList();

        foreach (var behavior in orderedBehaviors)
        {
            logger.LogDebug("Executing PreRun behavior: {BehaviorName}", behavior.Name);

            try
            {
                await behavior.ExecuteAsync(context);
                logger.LogDebug("PreRun behavior completed successfully: {BehaviorName}", behavior.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PreRun behavior failed: {BehaviorName}", behavior.Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Executes all PostRun behaviors in order.
    /// </summary>
    private async Task ExecutePostRunBehaviors(
        MappingContext context, 
        ContractsMappingResult result)
    {
        var orderedBehaviors = postRunBehaviors
            .OrderBy(b => b.Order)
            .ToList();

        foreach (var behavior in orderedBehaviors)
        {
            logger.LogDebug("Executing PostRun behavior: {BehaviorName}", behavior.Name);

            try
            {
                await behavior.ExecuteAsync(context, result);
                logger.LogDebug("PostRun behavior completed successfully: {BehaviorName}", behavior.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PostRun behavior failed: {BehaviorName}", behavior.Name);
            }
        }
    }
}