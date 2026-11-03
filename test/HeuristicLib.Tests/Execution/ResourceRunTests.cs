using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Execution;

public sealed class ResourceRunTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public async Task PauseKeepsPrivateFieldsAndSuppliedResource()
    {
        var handle = new CountAccumulatorHandle();
        var resource = new CountAccumulator();
        var algorithm = new ResourceAlgorithm(handle);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), setup: setup => setup.AddResource(handle, resource));
        run.Resource(handle).ShouldBeSameAs(resource);
        await using (var stream = run.Stream().GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            (await stream.MoveNextAsync()).ShouldBeTrue();
            stream.Current.EvaluatedCandidate.Candidate.ShouldBe(1);
        }
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        resource.CurrentCount.ShouldBe(1);
        var remaining = await run.Stream().ToListAsync(TestContext.Current.CancellationToken);
        remaining.Select(state => state.EvaluatedCandidate.Candidate).ShouldBe([2, 3]);
        resource.CurrentCount.ShouldBe(3);
        run.Resource(handle).ShouldBeSameAs(resource);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
    }

    [Fact]
    public void PerTrialSupplyMaterializesBeforeBindingAndCallbackFailureInstallsNothing()
    {
        var handle = new CountAccumulatorHandle();
        var calls = 0;
        var run = new ResourceAlgorithm(handle).Repeat(3).CreateRun(problem, RandomNumberGenerator.Create(1), setup =>
        {
            Should.Throw<InvalidOperationException>(() => setup.AddResourcePerTrial<CountAccumulator>(algorithm =>
                ++calls == 2 ? throw new InvalidOperationException("setup") : (algorithm.Counter, new CountAccumulator())));
            calls.ShouldBe(2);
            setup.AddResourcePerTrial(algorithm => (algorithm.Counter, new CountAccumulator()));
            Should.Throw<InvalidOperationException>(() => setup.AddResourcePerTrial(algorithm => (algorithm.Counter, new CountAccumulator())));
        });
        run.Trials.ShouldAllBe(trial => trial.Run.LifecycleState == RunLifecycleState.Ready);
    }

    [Fact]
    public void RunResourceLookupSharesWithItsExecutorsAndKeepsRunsIndependent()
    {
        var handle = new CountAccumulatorHandle();
        var algorithm = new ResourceAlgorithm(handle);
        var first = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        var second = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        var count = first.Resource(handle);
        count.CurrentCount.ShouldBe(0);

        first.Complete(cancellationToken: TestContext.Current.CancellationToken);

        first.Resource(handle).ShouldBeSameAs(count);
        count.CurrentCount.ShouldBe(3);
        second.Resource(handle).ShouldNotBeSameAs(count);
        second.Resource(handle).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void AlgorithmAdviceUsesTheSameResourceAsTheRunAndItsExecutor()
    {
        var handle = new CountAccumulatorHandle();
        var algorithm = new ResourceAlgorithm(handle);
        CountAccumulator? observed = null;
        var counter = new CountAccumulator();
        var advice = new ResourceAlgorithmAdvice(counter, resource => observed = resource);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1),
            setup => setup.AddResource(handle, counter).AddAspect(Aspect.Create(Pointcut.Reference(algorithm), advice)));
        run.Resource(handle).ShouldBeSameAs(counter);
        observed.ShouldBeNull();

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        observed.ShouldBeSameAs(counter);
        counter.CurrentCount.ShouldBe(3);
    }

    private sealed class ResourceAlgorithmAdvice(CountAccumulator counter, Action<CountAccumulator> observe)
        : AlgorithmAdvice<int, SingleSolutionState<int>>
    {
        public override IAlgorithmExecutor<int, TSearchSpace, TProblem, SingleSolutionState<int>>
            Wrap<TSearchSpace, TProblem>(
                IAlgorithmExecutor<int, TSearchSpace, TProblem, SingleSolutionState<int>> executor,
                IAlgorithm<int, SingleSolutionState<int>> source)
        {
            observe(counter);
            return executor;
        }
    }

    [Fact]
    public void DirectConstructorUsesTheSameResourceSetup()
    {
        var handle = new CountAccumulatorHandle();
        var resource = new CountAccumulator();
        var run = new AlgorithmRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SingleSolutionState<int>>(
            new ResourceAlgorithm(handle), problem, RandomNumberGenerator.Create(1), setup => setup.AddResource(handle, resource));

        resource.CurrentCount.ShouldBe(0);
        run.LifecycleState.ShouldBe(RunLifecycleState.Ready);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        resource.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void PerTrialSupplyChecksEveryBindingBeforeInstallingAny()
    {
        var first = new CountAccumulatorHandle();
        var second = new CountAccumulatorHandle();
        var run = new ResourceAlgorithm(first).Repeat(2).CreateRun(problem, RandomNumberGenerator.Create(1), setup =>
        {
            var index = 0;
            setup.AddResourcePerTrial(_ => (++index == 1 ? second : first, new CountAccumulator()));
            Should.Throw<InvalidOperationException>(() => setup.AddResourcePerTrial(_ => (first, new CountAccumulator())));
            index = 0;
            setup.AddResourcePerTrial(_ => (++index == 1 ? first : second, new CountAccumulator()));
        });

        run.Complete(cancellationToken: TestContext.Current.CancellationToken).Length.ShouldBe(2);
    }

    [Fact]
    public void CapturedSetupCannotSupplyAResourceToAnExistingRun()
    {
        var handle = new SuppliedResourceHandle<CountAccumulator>();
        AlgorithmRunSetup<int, SingleSolutionState<int>> captured = null!;
        var run = new ResourceAlgorithm(handle).CreateRun(problem, RandomNumberGenerator.Create(1), setup => captured = setup);
        captured.AddResource(handle, new CountAccumulator());

        Should.Throw<InvalidOperationException>(() => run.Complete(cancellationToken: TestContext.Current.CancellationToken))
            .Message.ShouldContain("explicit supply");
    }

    [Fact]
    public void DuplicateResourceSupplyFailsDuringCreateRun()
    {
        var handle = new CountAccumulatorHandle();
        Should.Throw<InvalidOperationException>(() => new ResourceAlgorithm(handle).CreateRun(problem, RandomNumberGenerator.Create(1),
            setup => setup.AddResource(handle, new CountAccumulator()).AddResource(handle, new CountAccumulator())));
    }

    [Fact]
    public async Task GridEqualityUsesHandleIdentityAndEachTrialOwnsItsResource()
    {
        var first = new CountAccumulatorHandle();
        var second = new CountAccumulatorHandle();
        var algorithm = new ResourceAlgorithm(first);
        var grid = algorithm.AsGrid().VaryBy([first, second], (configuration, handle) => configuration with { Counter = handle });
        var run = grid.CreateRun(problem, RandomNumberGenerator.Create(1));
        var results = await run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);
        results.Select(result => result.State.EvaluatedCandidate.ObjectiveVector[0]).ShouldBe([3.0, 3.0]);
        Should.Throw<InvalidOperationException>(() => algorithm.AsGrid()
            .VaryBy([first, first], (configuration, handle) => configuration with { Counter = handle }).MaterializeCases());
        (algorithm with { }).Counter.ShouldBeSameAs(first);
    }

    private sealed record ResourceAlgorithm(ResourceHandle<CountAccumulator> Counter)
        : Algorithm<ResourceAlgorithm, int, SingleSolutionState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, SingleSolutionState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, SingleSolutionState<int>> scope) => new Executor<TSearchSpace, TProblem>(scope.Resource(Counter));

        private sealed class Executor<TSearchSpace, TProblem>(CountAccumulator counter)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, SingleSolutionState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            private int calls;
            public override async IAsyncEnumerable<SingleSolutionState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                SingleSolutionState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < 3; i++)
                {
                    counter.IncrementBy(1);
                    yield return SingleSolutionState.From(++calls, new ObjectiveVector(counter.CurrentCount));
                }
                await Task.CompletedTask;
            }
        }
    }
}
