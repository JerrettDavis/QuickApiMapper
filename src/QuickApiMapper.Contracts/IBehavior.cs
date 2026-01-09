namespace QuickApiMapper.Contracts;

/// <summary>
/// Base interface for all behaviors.
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// The name of the behavior for logging and identification.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The order in which this behavior should be executed (lower values execute first).
    /// </summary>
    int Order { get; }
}

/// <summary>
/// Behavior that executes before the mapping process starts.
/// </summary>
public interface IPreRunBehavior : IBehavior
{
    /// <summary>
    /// Executes before the mapping process starts.
    /// </summary>
    /// <param name="context">The mapping context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(MappingContext context);
}

/// <summary>
/// Behavior that executes after the mapping process completes.
/// </summary>
public interface IPostRunBehavior : IBehavior
{
    /// <summary>
    /// Executes after the mapping process completes.
    /// </summary>
    /// <param name="context">The mapping context.</param>
    /// <param name="result">The mapping result.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(MappingContext context, MappingResult result);
}

/// <summary>
/// Behavior that wraps the entire mapping process.
/// </summary>
public interface IWholeRunBehavior : IBehavior
{
    /// <summary>
    /// Wraps the entire mapping process.
    /// </summary>
    /// <param name="context">The mapping context.</param>
    /// <param name="next">The next behavior in the pipeline or the core mapping logic.</param>
    /// <returns>A task that represents the asynchronous operation and returns the mapping result.</returns>
    Task<MappingResult> ExecuteAsync(MappingContext context, Func<MappingContext, Task<MappingResult>> next);
}