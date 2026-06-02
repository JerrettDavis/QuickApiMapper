using Microsoft.Extensions.Logging;
using PatternKit.Behavioral.Chain;
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
        await ExecutePreRunBehaviors(context);
        var result = await ExecuteWholeRunWithPostRunBehaviors(context, coreLogic);

        logger.LogInformation("Behavior pipeline execution completed. Success: {Success}", result.IsSuccess);
        return result;
    }

    private async Task<ContractsMappingResult> ExecuteWholeRunWithPostRunBehaviors(
        MappingContext context,
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        try
        {
            var result = await ExecuteWholeRunBehaviors(context, coreLogic);

            await ExecutePostRunBehaviors(context, result);

            return result;
        }
        catch (Exception ex) when (IsNonFatalPipelineException(ex))
        {
            var failureResult = ContractsMappingResult.Failure("Core mapping logic failed", ex);

            try
            {
                await ExecutePostRunBehaviors(context, failureResult);
            }
            catch (Exception postRunEx) when (IsNonFatalPipelineException(postRunEx))
            {
                logger.LogError(postRunEx, "PostRun behavior execution failed after core logic failure");
            }

            return failureResult;
        }
    }

    private async Task<ContractsMappingResult> ExecuteWholeRunBehaviors(
        MappingContext context,
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        var state = new BehaviorExecutionState(context, coreLogic);
        var builder = AsyncActionChain<BehaviorExecutionState>.Create();

        foreach (var behavior in wholeRunBehaviors.OrderBy(b => b.Order))
        {
            builder.Use(async (current, ct, next) =>
            {
                logger.LogDebug("Executing WholeRun behavior: {BehaviorName}", behavior.Name);
                current.Result = await behavior.ExecuteAsync(current.Context, ContinueAsync).ConfigureAwait(false);

                async Task<ContractsMappingResult> ContinueAsync(MappingContext nextContext)
                {
                    var previousContext = current.Context;
                    current.Context = nextContext;

                    try
                    {
                        await next(current, ct).ConfigureAwait(false);
                        return current.Result ?? CreateMissingResultFailure();
                    }
                    finally
                    {
                        current.Context = previousContext;
                    }
                }
            });
        }

        builder.Finally(async (current, _) =>
        {
            current.Result = await current.CoreLogic(current.Context).ConfigureAwait(false);
        });

        await builder.Build().ExecuteAsync(state, context.CancellationToken).ConfigureAwait(false);

        return state.Result ?? CreateMissingResultFailure();
    }

    private async Task ExecutePreRunBehaviors(MappingContext context)
    {
        var builder = AsyncActionChain<MappingContext>.Create();

        foreach (var behavior in preRunBehaviors.OrderBy(b => b.Order))
        {
            builder.Use(async (current, ct, next) =>
            {
                logger.LogDebug("Executing PreRun behavior: {BehaviorName}", behavior.Name);

                try
                {
                    await behavior.ExecuteAsync(current).ConfigureAwait(false);
                    logger.LogDebug("PreRun behavior completed successfully: {BehaviorName}", behavior.Name);
                }
                catch (Exception ex) when (IsNonFatalPipelineException(ex))
                {
                    logger.LogError(ex, "PreRun behavior failed: {BehaviorName}", behavior.Name);
                    throw;
                }

                await next(current, ct).ConfigureAwait(false);
            });
        }

        await builder.Build().ExecuteAsync(context, context.CancellationToken).ConfigureAwait(false);
    }

    private async Task ExecutePostRunBehaviors(
        MappingContext context,
        ContractsMappingResult result)
    {
        var state = new PostRunBehaviorExecutionState(context, result);
        var builder = AsyncActionChain<PostRunBehaviorExecutionState>.Create();

        foreach (var behavior in postRunBehaviors.OrderBy(b => b.Order))
        {
            builder.Use(async (current, ct, next) =>
            {
                logger.LogDebug("Executing PostRun behavior: {BehaviorName}", behavior.Name);

                try
                {
                    await behavior.ExecuteAsync(current.Context, current.Result).ConfigureAwait(false);
                    logger.LogDebug("PostRun behavior completed successfully: {BehaviorName}", behavior.Name);
                }
                catch (Exception ex) when (IsNonFatalPipelineException(ex))
                {
                    logger.LogError(ex, "PostRun behavior failed: {BehaviorName}", behavior.Name);
                }

                await next(current, ct).ConfigureAwait(false);
            });
        }

        await builder.Build().ExecuteAsync(state, context.CancellationToken).ConfigureAwait(false);
    }

    private static ContractsMappingResult CreateMissingResultFailure()
        => ContractsMappingResult.Failure("Behavior pipeline completed without producing a mapping result");

    private static bool IsNonFatalPipelineException(Exception exception)
        => exception is not OperationCanceledException
            and not OutOfMemoryException
            and not StackOverflowException
            and not AccessViolationException
            and not AppDomainUnloadedException
            and not BadImageFormatException;

    private sealed class BehaviorExecutionState(
        MappingContext context,
        Func<MappingContext, Task<ContractsMappingResult>> coreLogic)
    {
        public MappingContext Context { get; set; } = context;
        public Func<MappingContext, Task<ContractsMappingResult>> CoreLogic { get; } = coreLogic;
        public ContractsMappingResult? Result { get; set; }
    }

    private sealed class PostRunBehaviorExecutionState(
        MappingContext context,
        ContractsMappingResult result)
    {
        public MappingContext Context { get; } = context;
        public ContractsMappingResult Result { get; } = result;
    }
}
