using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Application.Core;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.UnitTests.Infrastructure;

/// <summary>
/// Test helper for managing behavior collections in unit tests.
/// </summary>
public class BehaviorTestCollection
{
    private readonly List<IPreRunBehavior> _preRunBehaviors = new();
    private readonly List<IPostRunBehavior> _postRunBehaviors = new();
    private readonly List<IWholeRunBehavior> _wholeRunBehaviors = new();

    /// <summary>
    /// Adds a PreRun behavior to the collection.
    /// </summary>
    public BehaviorTestCollection AddPreRunBehavior(IPreRunBehavior behavior)
    {
        _preRunBehaviors.Add(behavior);
        return this;
    }

    /// <summary>
    /// Adds a PostRun behavior to the collection.
    /// </summary>
    public BehaviorTestCollection AddPostRunBehavior(IPostRunBehavior behavior)
    {
        _postRunBehaviors.Add(behavior);
        return this;
    }

    /// <summary>
    /// Adds a WholeRun behavior to the collection.
    /// </summary>
    public BehaviorTestCollection AddWholeRunBehavior(IWholeRunBehavior behavior)
    {
        _wholeRunBehaviors.Add(behavior);
        return this;
    }

    /// <summary>
    /// Adds multiple PreRun behaviors to the collection.
    /// </summary>
    public BehaviorTestCollection AddPreRunBehaviors(params IPreRunBehavior[] behaviors)
    {
        _preRunBehaviors.AddRange(behaviors);
        return this;
    }

    /// <summary>
    /// Adds multiple PostRun behaviors to the collection.
    /// </summary>
    public BehaviorTestCollection AddPostRunBehaviors(params IPostRunBehavior[] behaviors)
    {
        _postRunBehaviors.AddRange(behaviors);
        return this;
    }

    /// <summary>
    /// Adds multiple WholeRun behaviors to the collection.
    /// </summary>
    public BehaviorTestCollection AddWholeRunBehaviors(params IWholeRunBehavior[] behaviors)
    {
        _wholeRunBehaviors.AddRange(behaviors);
        return this;
    }

    /// <summary>
    /// Clears all behaviors from the collection.
    /// </summary>
    public BehaviorTestCollection Clear()
    {
        _preRunBehaviors.Clear();
        _postRunBehaviors.Clear();
        _wholeRunBehaviors.Clear();
        return this;
    }

    /// <summary>
    /// Creates a BehaviorPipeline with the configured behaviors.
    /// </summary>
    public BehaviorPipeline BuildPipeline(ILogger<BehaviorPipeline> logger)
    {
        return new BehaviorPipeline(
            _preRunBehaviors,
            _postRunBehaviors,
            _wholeRunBehaviors,
            logger
        );
    }

    /// <summary>
    /// Creates a BehaviorPipeline with the configured behaviors using a service provider to get the logger.
    /// </summary>
    public BehaviorPipeline BuildPipeline(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<BehaviorPipeline>>();
        return BuildPipeline(logger);
    }

    /// <summary>
    /// Gets the current PreRun behaviors collection.
    /// </summary>
    public IReadOnlyList<IPreRunBehavior> PreRunBehaviors => _preRunBehaviors.AsReadOnly();

    /// <summary>
    /// Gets the current PostRun behaviors collection.
    /// </summary>
    public IReadOnlyList<IPostRunBehavior> PostRunBehaviors => _postRunBehaviors.AsReadOnly();

    /// <summary>
    /// Gets the current WholeRun behaviors collection.
    /// </summary>
    public IReadOnlyList<IWholeRunBehavior> WholeRunBehaviors => _wholeRunBehaviors.AsReadOnly();
}

/// <summary>
/// Extension methods for easier test setup.
/// </summary>
public static class BehaviorTestExtensions
{
    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial PreRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithPreRunBehavior(this BehaviorTestCollection collection, IPreRunBehavior behavior)
    {
        return collection.AddPreRunBehavior(behavior);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial PostRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithPostRunBehavior(this BehaviorTestCollection collection, IPostRunBehavior behavior)
    {
        return collection.AddPostRunBehavior(behavior);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial WholeRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithWholeRunBehavior(this BehaviorTestCollection collection, IWholeRunBehavior behavior)
    {
        return collection.AddWholeRunBehavior(behavior);
    }
}

/// <summary>
/// Factory class for creating BehaviorTestCollection instances.
/// </summary>
public static class BehaviorTestCollectionFactory
{
    /// <summary>
    /// Creates a new empty BehaviorTestCollection.
    /// </summary>
    public static BehaviorTestCollection Create()
    {
        return new BehaviorTestCollection();
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial PreRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithPreRunBehavior(IPreRunBehavior behavior)
    {
        return new BehaviorTestCollection().AddPreRunBehavior(behavior);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial PostRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithPostRunBehavior(IPostRunBehavior behavior)
    {
        return new BehaviorTestCollection().AddPostRunBehavior(behavior);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with an initial WholeRun behavior.
    /// </summary>
    public static BehaviorTestCollection WithWholeRunBehavior(IWholeRunBehavior behavior)
    {
        return new BehaviorTestCollection().AddWholeRunBehavior(behavior);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with multiple PreRun behaviors.
    /// </summary>
    public static BehaviorTestCollection WithPreRunBehaviors(params IPreRunBehavior[] behaviors)
    {
        return new BehaviorTestCollection().AddPreRunBehaviors(behaviors);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with multiple PostRun behaviors.
    /// </summary>
    public static BehaviorTestCollection WithPostRunBehaviors(params IPostRunBehavior[] behaviors)
    {
        return new BehaviorTestCollection().AddPostRunBehaviors(behaviors);
    }

    /// <summary>
    /// Creates a new BehaviorTestCollection with multiple WholeRun behaviors.
    /// </summary>
    public static BehaviorTestCollection WithWholeRunBehaviors(params IWholeRunBehavior[] behaviors)
    {
        return new BehaviorTestCollection().AddWholeRunBehaviors(behaviors);
    }
}