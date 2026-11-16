using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

// Covers the run's results: one per instrument, created at the first lookup and kept for the run, and the time the
// root algorithm has spent executing.
public sealed class RunResultsTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public void AnInstrumentHasOneResultPerRun_CreatedAtItsFirstLookup()
    {
        var instrument = new CreationCountingInstrument();
        var first = ResolutionScope.Create();
        var second = ResolutionScope.Create();

        instrument.Created.ShouldBe(0);
        var result = first.Results.Result(instrument);

        first.CreateChildScope().Results.Result(instrument).ShouldBeSameAs(result);
        second.Results.Result(instrument).ShouldNotBeSameAs(result);
        instrument.Created.ShouldBe(2);
    }

    [Fact]
    public async Task ConcurrentLookupsAcrossChildScopesPublishOneResult()
    {
        var instrument = new CreationCountingInstrument();
        var root = ResolutionScope.Create();
        var lookups = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(
            () => root.CreateChildScope().Results.Result(instrument),
            TestContext.Current.CancellationToken)));

        instrument.Created.ShouldBe(1);
        lookups.ShouldAllBe(value => ReferenceEquals(value, lookups[0]));
    }

    [Fact]
    public void FailedResultCreationPublishesNothingAndCanRetry()
    {
        var instrument = new FailsFirstCreation();
        var results = ResolutionScope.Create().Results;

        Should.Throw<InvalidOperationException>(() => results.Result(instrument));
        var result = results.Result(instrument);

        instrument.Attempts.ShouldBe(2);
        results.Result(instrument).ShouldBeSameAs(result);
        instrument.Attempts.ShouldBe(2);
    }

    [Fact]
    public void InstrumentsMatchByReference()
    {
        var results = ResolutionScope.Create().Results;
        var first = new CreationCountingInstrument();
        var second = new CreationCountingInstrument();

        first.ShouldNotBe(second);
        results.Result(first).ShouldNotBeSameAs(results.Result(second));
    }

    [Fact]
    public void AnInstrumentBuildingOnItself_IsRejected()
    {
        var results = ResolutionScope.Create().Results;

        Should.Throw<InvalidOperationException>(() => results.Result(new SelfReferencingInstrument()))
            .Message.ShouldContain("requires that result itself");
    }

    [Fact]
    public async Task ResultsStayReadableAfterAConstructionFailure()
    {
        var evaluator = new ProblemEvaluator<int>();
        var evaluations = evaluator.Invocations.Count();
        var algorithm = new AdditiveStepAlgorithm(1) { Evaluator = evaluator }.AnalyzedBy(evaluations);
        var root = ResolutionScope.Create();
        await Run(Resolve(root, algorithm));

        Should.Throw<InvalidOperationException>(() => Resolve(root, new AdditiveStepAlgorithm(1) { Evaluator = new FailingEvaluator() }));

        root.Results.Result(evaluations).CurrentCount.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => Resolve(root, algorithm)).Message.ShouldContain("failed in this resolution scope tree");
    }

    [Fact]
    public async Task Elapsed_CountsOnlyCallsAdvancingTheRunIterator()
    {
        var time = new ManualTimeProvider();
        var run = CreateRun(new SteppingAlgorithm(time, Steps: 3), time);
        var results = run.Result(new ResultsInstrument());
        time.Advance(TimeSpan.FromSeconds(100));
        results.Elapsed.ShouldBe(TimeSpan.Zero);

        await using var states = run.Stream(cancellationToken: TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        results.Elapsed.ShouldBe(TimeSpan.Zero);
        time.Advance(TimeSpan.FromSeconds(100));
        (await states.MoveNextAsync()).ShouldBeTrue();
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(100));
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        (await states.MoveNextAsync()).ShouldBeTrue();
        (await states.MoveNextAsync()).ShouldBeTrue();
        (await states.MoveNextAsync()).ShouldBeFalse();

        // The final call takes two seconds without yielding a state.
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        time.Advance(TimeSpan.FromSeconds(100));
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DirectScopeExecution_LeavesElapsedAndElapsedClockAtZero()
    {
        var time = new ManualTimeProvider();
        var algorithm = new SteppingAlgorithm(time, Steps: 2);
        var steps = algorithm.Iterations.Trace(observation => observation.Iteration, [Clock.Elapsed]);
        var duration = new Duration(time);
        var root = ResolutionScope.Create(time);
        var configured = algorithm.AnalyzedBy(steps).Timed(duration);
        var executor = Resolve(root, configured);
        Resolve(root, configured).ShouldBeSameAs(executor);

        await Run(executor);

        root.Results.Elapsed.ShouldBe(TimeSpan.Zero);
        root.Results.Result(steps).By(Clock.Elapsed).Select(point => point.Time)
            .ShouldBe([TimeSpan.Zero, TimeSpan.Zero]);
        root.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(4));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Elapsed_PauseAndCancellationExcludeTimeBeforeResume(bool cancel)
    {
        var time = new ManualTimeProvider();
        var algorithm = new SteppingAlgorithm(time, Steps: 3);
        var steps = algorithm.Iterations.Trace(observation => observation.Iteration, [Clock.Elapsed]);
        var run = CreateRun(algorithm.AnalyzedBy(steps), time);
        var results = run.Result(new ResultsInstrument());
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await using (var states = run.Stream(cancellationToken: cancellation.Token).GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            (await states.MoveNextAsync()).ShouldBeTrue();
            if (cancel)
            {
                await cancellation.CancelAsync();
                time.Advance(TimeSpan.FromSeconds(100));
                await Should.ThrowAsync<OperationCanceledException>(async () => await states.MoveNextAsync());
            }
        }

        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(100));
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        var remaining = await run.Stream(cancellationToken: TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        remaining.Select(state => state.Population.EvaluatedCandidates.Single().Candidate).ShouldBe([2, 3]);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
        run.Result(steps).By(Clock.Elapsed).Select(point => point.Time)
            .ShouldBe([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Elapsed_StaysLiveDuringAnAwaitAndStopsAfterFailureOrTerminalCancellation(bool canceled)
    {
        var time = new ManualTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = CreateRun(new GatedAlgorithm(gate.Task), time);
        var results = run.Result(new ResultsInstrument());
        var readClock = Clock.Elapsed.Bind(results);
        await using var states = run.StreamForTerminalCancellation(null, TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var move = states.MoveNextAsync();
        time.Advance(TimeSpan.FromSeconds(5));

        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
        readClock().ShouldBe(TimeSpan.FromSeconds(5));
        Exception failure = canceled ? new OperationCanceledException() : new InvalidOperationException("Execution failed.");
        gate.SetException(failure);
        var thrown = await Record.ExceptionAsync(async () => await move);

        thrown.ShouldBeSameAs(failure);
        run.LifecycleState.ShouldBe(canceled ? RunLifecycleState.Canceled : RunLifecycleState.Failed);
        time.Advance(TimeSpan.FromSeconds(100));
        results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
        readClock().ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ElapsedClock_ReadsTheTimeOfEachObservation()
    {
        var time = new ManualTimeProvider();
        var algorithm = new SteppingAlgorithm(time, Steps: 3);
        var steps = algorithm.Iterations.Trace(observation => observation.Iteration, [Clock.Elapsed]);
        var run = CreateRun(algorithm.AnalyzedBy(steps), time);

        await foreach (var _ in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
            time.Advance(TimeSpan.FromSeconds(10));

        run.Result(steps).By(Clock.Elapsed).Select(point => point.Time)
            .ShouldBe([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)]);
    }

    private static AlgorithmRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateRun(
        IAlgorithm<int, PopulationState<int>> algorithm, TimeProvider time) =>
        new(algorithm, Problem, RandomNumberGenerator.Create(1), time);

    private sealed class ResultsInstrument : Instrument<RunResults>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        protected override RunResults CreateResult(RunResults results) => results;
    }

    private static IAlgorithmExecutor<PopulationState<int>> Resolve(
        ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, Problem);

    private static async Task<List<PopulationState<int>>> Run(
        IAlgorithmExecutor<PopulationState<int>> executor)
    {
        var states = new List<PopulationState<int>>();
        await foreach (var state in executor.RunStreamingAsync(RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        return states;
    }

    // One side creates a result that reads a counter, while the other builds a configuration that reads that result.
    [Fact]
    public async Task ReadingAResult_AndBuildingAConfigurationThatReadsIt_DoNotDeadlock()
    {
        using var creating = new ManualResetEventSlim();
        using var building = new ManualResetEventSlim();
        var definition = new ReadsACounter(new Counter(), () =>
        {
            creating.Set();
            building.Wait(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
        });
        var scope = ResolutionScope.Create();

        var read = Task.Run(() => scope.Results.Result(definition), TestContext.Current.CancellationToken);
        var build = Task.Run(() =>
        {
            creating.Wait(TestContext.Current.CancellationToken);
            return scope.Resolve(new ReadsAResult(definition, building.Set));
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(read, build).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    // The configuration decides who owns its result, whatever type a caller holds it by.
    [Fact]
    public void ACounterHeldByAWiderType_StillCountsForTheRun()
    {
        var counter = new Counter();
        IConfigurationNode<IExecutor> widened = counter;
        var root = ResolutionScope.Create();

        ((CountAccumulator)root.CreateChildScope().Resolve(widened)).IncrementBy(1);

        root.Results.Result(counter).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void AConfigurationReadingItsOwnResult_IsAnErrorInsteadOfAStackOverflow()
    {
        var node = new ReadsItself();

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Results.Result(node)).Message.ShouldContain("Recursive construction");
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve(node)).Message.ShouldContain("Recursive construction");
    }

    // Nothing has built this counter, and its result is readable all the same.
    [Fact]
    public void AResultOfAFailedRun_StaysReadableEvenIfNeverBuilt()
    {
        var scope = ResolutionScope.Create();
        Should.Throw<InvalidOperationException>(() => scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(new FailingEvaluator(), Problem));

        scope.Results.Result(new Counter()).CurrentCount.ShouldBe(0);
    }

    private sealed record ReadsItself : IConfigurationNode<CountAccumulator>
    {
        public CountAccumulator CreateExecutor(ResolutionScope scope) => scope.Results.Result(this);
    }

    private sealed class ReadsACounter(Counter counter, Action creating) : Instrument<object>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        protected override object CreateResult(RunResults results)
        {
            creating();
            return results.Result(counter);
        }
    }

    private sealed record ReadsAResult(Instrument<object> Definition, Action Building) : IConfigurationNode<CountAccumulator>
    {
        public CountAccumulator CreateExecutor(ResolutionScope scope)
        {
            Building();
            scope.Results.Result(Definition);
            return new CountAccumulator();
        }
    }

    private sealed class CreationCountingInstrument : Instrument<object>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        public int Created { get; private set; }

        protected override object CreateResult(RunResults results)
        {
            Created++;
            return new object();
        }
    }

    private sealed class FailsFirstCreation : Instrument<object>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        public int Attempts { get; private set; }

        protected override object CreateResult(RunResults results)
        {
            Attempts++;
            if (Attempts == 1)
                throw new InvalidOperationException("result creation");
            return new object();
        }
    }

    private sealed class SelfReferencingInstrument : Instrument<object>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [];

        protected override object CreateResult(RunResults results) => results.Result(this);
    }

    private sealed record FailingEvaluator : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope) =>
            throw new InvalidOperationException("Evaluator construction failed.");
    }

    // Spends one second inside each call that produces a state.
    private sealed record SteppingAlgorithm(ManualTimeProvider Time, int Steps) : Algorithm<SteppingAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            Time.Advance(TimeSpan.FromSeconds(100));
            return new Executor(Time, Steps);
        }

        private sealed class Executor(ManualTimeProvider time, int steps)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var step = 1; step <= steps; step++)
                {
                    time.Advance(TimeSpan.FromSeconds(1));
                    yield return Population.From([step.ToEvaluated(new ObjectiveVector(step))]).ToPopulationState();
                }
                time.Advance(TimeSpan.FromSeconds(2));
                await Task.CompletedTask;
            }
        }
    }

    // Produces one state once the gate opens.
    private sealed record GatedAlgorithm(Task Gate) : Algorithm<GatedAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(Gate);

        private sealed class Executor(Task gate)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await gate;
                yield return Population.From([1.ToEvaluated(new ObjectiveVector(1))]).ToPopulationState();
            }
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref timestamp);

        public void Advance(TimeSpan by) => Interlocked.Add(ref timestamp, by.Ticks);
    }
}
