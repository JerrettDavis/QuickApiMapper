using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using QuickApiMapper.Behaviors;
using QuickApiMapper.Contracts;
using QuickApiMapper.UnitTests.Infrastructure;

namespace QuickApiMapper.UnitTests;

[TestFixture]
public class SimpleBehaviorTest
{
    [Test]
    public async Task BehaviorPipeline_WithTestBehaviors_ShouldExecuteInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        var serviceProvider = services.BuildServiceProvider();
        
        var executionOrder = new List<string>();
        var testPreRunBehavior = new TestPreRunBehavior(executionOrder);
        var testPostRunBehavior = new TestPostRunBehavior(executionOrder);
        var testWholeRunBehavior = new TestWholeRunBehavior(executionOrder);
        
        // Use the BehaviorTestCollection to build a pipeline
        var pipeline = BehaviorTestCollectionFactory.Create()
            .AddPreRunBehavior(testPreRunBehavior)
            .AddPostRunBehavior(testPostRunBehavior)
            .AddWholeRunBehavior(testWholeRunBehavior)
            .BuildPipeline(serviceProvider);
        
        var context = new MappingContext
        {
            Mappings = [new FieldMapping("test.source", "test.destination")],
            Source = new { test = "data" },
            ServiceProvider = serviceProvider,
            CancellationToken = CancellationToken.None
        };
        
        // Act
        var result = await pipeline.ExecuteAsync(context, _ =>
        {
            executionOrder.Add("CoreLogic");
            return Task.FromResult(MappingResult.Success());
        });
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(executionOrder, Is.EqualTo(new[] { "PreRun", "WholeRun-Start", "CoreLogic", "WholeRun-End", "PostRun" }));
        });
    }
    
    [Test]
    public void BehaviorTestCollection_CanAddMultipleBehaviors()
    {
        // Arrange
        var executionOrder = new List<string>();
        var preRun1 = new TestPreRunBehavior(executionOrder, "PreRun1");
        var preRun2 = new TestPreRunBehavior(executionOrder, "PreRun2");
        var postRun1 = new TestPostRunBehavior(executionOrder, "PostRun1");
        var postRun2 = new TestPostRunBehavior(executionOrder, "PostRun2");
        
        // Act
        var collection = BehaviorTestCollectionFactory.Create()
            .AddPreRunBehavior(preRun1)
            .AddPreRunBehavior(preRun2)
            .AddPostRunBehavior(postRun1)
            .AddPostRunBehavior(postRun2);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(collection.PreRunBehaviors.Count, Is.EqualTo(2));
            Assert.That(collection.PostRunBehaviors.Count, Is.EqualTo(2));
            Assert.That(collection.WholeRunBehaviors.Count, Is.EqualTo(0));
        });
    }
    
    [Test]
    public void BehaviorTestCollection_CanClearBehaviors()
    {
        // Arrange
        var executionOrder = new List<string>();
        var preRun = new TestPreRunBehavior(executionOrder);
        var postRun = new TestPostRunBehavior(executionOrder);
        
        var collection = BehaviorTestCollectionFactory.Create()
            .AddPreRunBehavior(preRun)
            .AddPostRunBehavior(postRun);
        
        // Act
        collection.Clear();
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(collection.PreRunBehaviors.Count, Is.EqualTo(0));
            Assert.That(collection.PostRunBehaviors.Count, Is.EqualTo(0));
            Assert.That(collection.WholeRunBehaviors.Count, Is.EqualTo(0));
        });
    }
    
    private class TestPreRunBehavior : IPreRunBehavior
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;
        
        public TestPreRunBehavior(List<string> executionOrder, string name = "PreRun")
        {
            _executionOrder = executionOrder;
            _name = name;
        }
        
        public string Name => _name;
        public int Order => 100;
        
        public Task ExecuteAsync(MappingContext context)
        {
            _executionOrder.Add(_name);
            return Task.CompletedTask;
        }
    }
    
    private class TestPostRunBehavior : IPostRunBehavior
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;
        
        public TestPostRunBehavior(List<string> executionOrder, string name = "PostRun")
        {
            _executionOrder = executionOrder;
            _name = name;
        }
        
        public string Name => _name;
        public int Order => 100;
        
        public Task ExecuteAsync(MappingContext context, MappingResult result)
        {
            _executionOrder.Add(_name);
            return Task.CompletedTask;
        }
    }
    
    private class TestWholeRunBehavior : IWholeRunBehavior
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;
        
        public TestWholeRunBehavior(List<string> executionOrder, string name = "WholeRun")
        {
            _executionOrder = executionOrder;
            _name = name;
        }
        
        public string Name => _name;
        public int Order => 100;
        
        public async Task<MappingResult> ExecuteAsync(MappingContext context, Func<MappingContext, Task<MappingResult>> next)
        {
            _executionOrder.Add($"{_name}-Start");
            var result = await next(context);
            _executionOrder.Add($"{_name}-End");
            return result;
        }
    }
}
