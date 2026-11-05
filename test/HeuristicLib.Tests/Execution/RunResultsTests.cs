using System.Runtime.CompilerServices;
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
        first.Results.Aspect(instrument).ShouldBeSameAs(first.Results.Aspect(instrument));
        second.Results.Result(instrument).ShouldNotBeSameAs(result);
        instrument.Created.ShouldBe(2);
    }

    [Fact]
    public async Task ConcurrentLookupsAcrossChildScopesPublishOneResultAndAspect()
    {
        var instrument = new CreationCountingInstrument();
        var root = ResolutionScope.Create();
        var lookups = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var results = root.CreateChildScope().Results;
            return (Result: results.Result(instrument), Aspect: results.Aspect(instrument));
        }, TestContext.Current.CancellationToken)));

        instrument.Created.ShouldBe(1);
        lookups.ShouldAllBe(value => ReferenceEquals(value.Result, lookups[0].Result) && ReferenceEquals(value.Aspect, lookups[0].Aspect));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedResultOrAspectCreationPublishesNeitherAndCanRetry(bool failAspect)
    {
        var instrument = new FailsFirstCreation(failAspect);
        var results = ResolutionScope.Create().Results;

        Should.Throw<InvalidOperationException>(() => results.Aspect(instrument));
        var result = results.Result(instrument);

        instrument.Attempts.ShouldBe(2);
        results.Result(instrument).ShouldBeSameAs(result);
        results.Aspect(instrument).ShouldBeSameAs(results.Aspect(instrument));
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
    public void AnInstrumentPlacedNowhere_HasAnEmptyAspect() =>
        ResolutionScope.Create().Results.Aspect(new CreationCountingInstrument()).ShouldBeEmpty();

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
        var algorithm = new AdditiveStepAlgorithm(1) { Evaluator = evaluator }.Advised(evaluations);
        var root = ResolutionScope.Create();
        await Run(Resolve(root, algorithm));

        Should.Throw<InvalidOperationException>(() => Resolve(root, new AdditiveStepAlgorithm(1) { Evaluator = new FailingEvaluator() }));

        root.Results.Result(evaluations).CurrentCount.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => Resolve(root, algorithm)).Message.ShouldContain("failed in this resolution scope tree");
    }

    // Elapsed time counts the time inside the root algorithm's calls only, so the time before it starts and the time a
    // consumer spends between states are excluded, and direct resolution without a run measures it too.
    [Fact]
    public async Task Elapsed_CountsOnlyTheTimeInsideTheRootAlgorithm()
    {
        var time = new ManualTimeProvider();
        var root = ResolutionScope.Create(time);
        var executor = Resolve(root, new SteppingAlgorithm(time, Steps: 3));
        time.Advance(TimeSpan.FromSeconds(100));

        await using var states = executor.RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await states.MoveNextAsync()).ShouldBeTrue();
        root.Results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        time.Advance(TimeSpan.FromSeconds(100));
        root.Results.Elapsed.ShouldBe(TimeSpan.FromSeconds(1));
        (await states.MoveNextAsync()).ShouldBeTrue();
        (await states.MoveNextAsync()).ShouldBeTrue();
        (await states.MoveNextAsync()).ShouldBeFalse();

        root.Results.Elapsed.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Elapsed_IsNotMeasuredBelowTheRoot()
    {
        var time = new ManualTimeProvider();
        var root = ResolutionScope.Create(time);

        await Run(Resolve(root.CreateChildScope(), new SteppingAlgorithm(time, Steps: 2)));

        root.Results.Elapsed.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public async Task Elapsed_CountsOverlappingRootCallsOnce()
    {
        var time = new ManualTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var root = ResolutionScope.Create(time);
        var algorithm = new GatedAlgorithm(gate.Task);
        await using var first = Resolve(root, algorithm).RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await using var second = Resolve(root, algorithm).RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var firstMove = first.MoveNextAsync();
        var secondMove = second.MoveNextAsync();
        time.Advance(TimeSpan.FromSeconds(5));
        gate.SetResult();
        (await firstMove).ShouldBeTrue();
        (await secondMove).ShouldBeTrue();

        root.Results.Elapsed.ShouldBe(TimeSpan.FromSeconds(5));
    }

    // Measuring elapsed time at the root keeps the root executor's own controls reachable.
    [Fact]
    public async Task TheRootAlgorithmsControls_PassThroughTheElapsedTimeMeasurement()
    {
        var executor = Resolve(ResolutionScope.Create(), new ControlledAlgorithm());

        ExecutorControls.Find<IStepControl>(executor).ShouldNotBeNull().Steps = 2;

        (await Run(executor)).Count.ShouldBe(2);
    }

    // A trace reading the elapsed clock records the time of each state inside the call that produced it.
    [Fact]
    public async Task ElapsedClock_ReadsTheTimeOfEachObservation()
    {
        var time = new ManualTimeProvider();
        var algorithm = new SteppingAlgorithm(time, Steps: 3);
        var steps = algorithm.Iterations.Trace(observation => observation.Iteration, [Clock.Elapsed]);
        var root = ResolutionScope.Create(time);
        var states = Resolve(root, algorithm.Advised(steps)).RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken);

        await foreach (var _ in states)
            time.Advance(TimeSpan.FromSeconds(10));

        root.Results.Result(steps).By(Clock.Elapsed).Select(point => point.Time)
            .ShouldBe([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)]);
    }

    private static IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Resolve(
        ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);

    private static async Task<List<PopulationState<int>>> Run(
        IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> executor)
    {
        var states = new List<PopulationState<int>>();
        await foreach (var state in executor.RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        return states;
    }

    private sealed class CreationCountingInstrument : Instrument<object>
    {
        public int Created { get; private set; }

        protected override object CreateResult(RunResults results)
        {
            Created++;
            return new object();
        }
    }

    private sealed class FailsFirstCreation(bool failAspect) : Instrument<object>
    {
        public int Attempts { get; private set; }

        protected override object CreateResult(RunResults results)
        {
            Attempts++;
            if (!failAspect && Attempts == 1)
                throw new InvalidOperationException("result creation");
            return new object();
        }

        protected override Aspect? CreateAspect(object result, RunResults results)
        {
            if (failAspect && Attempts == 1)
                throw new InvalidOperationException("aspect creation");
            return null;
        }
    }

    private sealed class SelfReferencingInstrument : Instrument<object>
    {
        protected override object CreateResult(RunResults results) => results.Result(this);
    }

    private sealed record FailingEvaluator : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        protected override IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
            throw new InvalidOperationException("Evaluator construction failed.");
    }

    // Spends one second inside each call that produces a state.
    private sealed record SteppingAlgorithm(ManualTimeProvider Time, int Steps) : Algorithm<SteppingAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor<TSearchSpace, TProblem>(Time, Steps);

        private sealed class Executor<TSearchSpace, TProblem>(ManualTimeProvider time, int steps)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var step = 1; step <= steps; step++)
                {
                    time.Advance(TimeSpan.FromSeconds(1));
                    yield return Population.From([step.ToEvaluated(new ObjectiveVector(step))]).ToPopulationState();
                }
                await Task.CompletedTask;
            }
        }
    }

    // Produces one state once the gate opens.
    private sealed record GatedAlgorithm(Task Gate) : Algorithm<GatedAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor<TSearchSpace, TProblem>(Gate);

        private sealed class Executor<TSearchSpace, TProblem>(Task gate)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await gate;
                yield return Population.From([1.ToEvaluated(new ObjectiveVector(1))]).ToPopulationState();
            }
        }
    }

    private interface IStepControl
    {
        int Steps { get; set; }
    }

    // Yields as many states as its executor's control asks for.
    private sealed record ControlledAlgorithm : Algorithm<ControlledAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor<TSearchSpace, TProblem>();

        private sealed class Executor<TSearchSpace, TProblem> : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>, IStepControl
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public int Steps { get; set; } = 1;

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var step = 1; step <= Steps; step++)
                    yield return Population.From([step.ToEvaluated(new ObjectiveVector(step))]).ToPopulationState();
                await Task.CompletedTask;
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
