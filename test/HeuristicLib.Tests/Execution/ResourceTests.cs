using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Execution;

public sealed class ResourceTests
{
    [Fact]
    public void ConsumerRoleAdviceReceivesTheResourceSuppliedToItsContext()
    {
        var handle = new CountAccumulatorHandle();
        var source = new ResourceNode(handle);
        var observed = new List<CountAccumulator>();
        var counter = new CountAccumulator();
        var aspect = Aspect.Create(Pointcut.Reference(source), new ResourceNodeAdvice(counter, observed.Add));
        var scope = new ResolutionScopeBuilder(aspect).AddResource(handle, counter).Build();
        var first = scope.Resolve(source);
        var second = scope.With().Resolve(source);
        var independentCounter = new CountAccumulator();
        var independentAspect = Aspect.Create(Pointcut.Reference(source), new ResourceNodeAdvice(independentCounter, observed.Add));
        var independent = new ResolutionScopeBuilder(independentAspect).AddResource(handle, independentCounter).Build().Resolve(source);

        first.Counter.ShouldBeSameAs(scope.Resource(handle));
        second.Counter.ShouldBeSameAs(first.Counter);
        independent.Counter.ShouldNotBeSameAs(first.Counter);
        observed.ShouldBe([first.Counter, second.Counter, independent.Counter]);
    }

    private interface IResourceNodeExecutor : IExecutor
    {
        CountAccumulator Counter { get; }
    }

    private sealed record ResourceNode(ResourceHandle<CountAccumulator> Counter) : IConfigurationNode<IResourceNodeExecutor>
    {
        public IResourceNodeExecutor CreateExecutor(ResolutionScope scope) => new ResourceNodeExecutor(scope.Resource(Counter));
    }

    private sealed class ResourceNodeExecutor(CountAccumulator counter) : IResourceNodeExecutor
    {
        public CountAccumulator Counter { get; } = counter;
    }

    private sealed class ResourceNodeAdvice(CountAccumulator counter, Action<CountAccumulator> observe)
        : Advice<ResourceNode, IResourceNodeExecutor>
    {
        public override IResourceNodeExecutor Wrap(IResourceNodeExecutor executor, ResourceNode source)
        {
            observe(counter);
            return executor;
        }
    }

    [Fact]
    public async Task ConcurrentRequestsPublishOneResourceAndSeparateRootsAreIndependent()
    {
        var creations = 0;
        var handle = new TestHandle(() => { Interlocked.Increment(ref creations); return new object(); });
        var root = ResolutionScope.Create();
        var resources = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => root.With().Resource(handle), TestContext.Current.CancellationToken)));
        creations.ShouldBe(1);
        resources.All(resource => ReferenceEquals(resource, resources[0])).ShouldBeTrue();
        ResolutionScope.Create().Resource(handle).ShouldNotBeSameAs(resources[0]);
        creations.ShouldBe(2);
    }

    [Fact]
    public void FailedCreationCanRetryAndLaterConstructionFailuresKeepPublishedResources()
    {
        var attempts = 0;
        var handle = new TestHandle(() => ++attempts == 1 ? throw new InvalidOperationException("creation") : new object());
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resource(handle));
        var resource = scope.Resource(handle);
        var broken = new BrokenNode(handle);
        Should.Throw<InvalidOperationException>(() => scope.Resolve(broken));
        scope.Resource(handle).ShouldBeSameAs(resource);
        attempts.ShouldBe(2);
    }

    [Fact]
    public void SuppliedResourcesAreBorrowedAndDuplicateSupplyFails()
    {
        using var resource = new OwnedResource();
        var handle = new SuppliedResourceHandle<OwnedResource>();
        var builder = new ResolutionScopeBuilder().AddResource(handle, resource);
        var scope = builder.Build();
        Should.Throw<InvalidOperationException>(() => builder.AddResource(handle, resource));
        scope.Resource(handle).ShouldBeSameAs(resource);
        builder.Build().Resource(handle).ShouldBeSameAs(resource);
        resource.Disposed.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resource(handle)).Message.ShouldContain("explicit supply");
    }

    [Fact]
    public async Task ConcurrentCacheMissesEvaluateOutsideTheSharedStoreAndPublishSafely()
    {
        using var overlap = new Barrier(2);
        var gate = new SuppliedResourceHandle<Barrier>();
        var store = new EvaluationCacheHandle<RealVector>();
        var source = new OverlappingEvaluator(gate).Cached(store);
        var scope = new ResolutionScopeBuilder().AddResource(gate, overlap).Build().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var first = scope.Resolve(source);
        var second = scope.With().Resolve(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var candidate = new RealVector(2.0);
        var results = await Task.WhenAll(
            Task.Run(() => first.Evaluate([candidate], RandomNumberGenerator.Create(1), problem.SearchSpace, problem), TestContext.Current.CancellationToken),
            Task.Run(() => second.Evaluate([candidate], RandomNumberGenerator.Create(2), problem.SearchSpace, problem), TestContext.Current.CancellationToken));
        results.Select(result => result[0][0]).ShouldBe([4.0, 4.0]);
        // No third participant reaches the barrier: this call must be a cache hit.
        first.Evaluate([candidate], RandomNumberGenerator.Create(3), problem.SearchSpace, problem)[0][0].ShouldBe(4.0);
    }

    [Fact]
    public void BuilderSnapshotsBindingsAndCreatesIndependentDefaultResources()
    {
        var handle = new TestHandle(() => new object());
        var builder = new ResolutionScopeBuilder();
        var first = builder.Build();
        var second = builder.Build();
        var supplied = new object();
        builder.AddResource(handle, supplied);
        var third = builder.Build();
        first.Resource(handle).ShouldNotBeSameAs(second.Resource(handle));
        first.Resource(handle).ShouldNotBeSameAs(supplied);
        second.Resource(handle).ShouldNotBeSameAs(supplied);
        third.Resource(handle).ShouldBeSameAs(supplied);
        first.With().Resource(handle).ShouldBeSameAs(first.Resource(handle));
    }

    [Fact]
    public void BuilderSnapshotsAspectsWithoutChangingExistingScopes()
    {
        var source = new ProblemEvaluator<RealVector>();
        var firstCounter = new CountAccumulator();
        var laterCounter = new CountAccumulator();
        var builder = new ResolutionScopeBuilder(Aspect.CountInvocations(source, firstCounter));
        var first = builder.Build();
        builder.AddAspect(Aspect.CountInvocations(source, laterCounter));
        var second = builder.Build();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var random = RandomNumberGenerator.Create(1);

        first.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)
            .Evaluate([new RealVector(2.0)], random, problem.SearchSpace, problem);
        firstCounter.CurrentCount.ShouldBe(1);
        laterCounter.CurrentCount.ShouldBe(0);
        second.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)
            .Evaluate([new RealVector(2.0)], random, problem.SearchSpace, problem);
        firstCounter.CurrentCount.ShouldBe(2);
        laterCounter.CurrentCount.ShouldBe(1);
    }

    private sealed class TestHandle(Func<object> create) : ResourceHandle<object>
    {
        protected internal override object CreateResource() => create();
    }

    private sealed record BrokenNode(ResourceHandle<object> Resource) : IConfigurationNode<IExecutor>
    {
        public IExecutor CreateExecutor(ResolutionScope scope)
        {
            _ = scope.Resource(Resource);
            throw new InvalidOperationException("executor construction");
        }
    }

    private sealed class OwnedResource : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() { Disposed = true; }
    }

    private sealed record OverlappingEvaluator(ResourceHandle<Barrier> Gate) : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor(ResolutionScope scope) => new Executor(scope.Resource(Gate));

        private sealed class Executor(Barrier gate) : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random,
                BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                if (!gate.SignalAndWait(TimeSpan.FromSeconds(10)))
                    throw new InvalidOperationException("Concurrent evaluations did not overlap.");
                return candidates.Select(candidate => new ObjectiveVector(candidate[0] * candidate[0])).ToArray();
            }
        }
    }
}
