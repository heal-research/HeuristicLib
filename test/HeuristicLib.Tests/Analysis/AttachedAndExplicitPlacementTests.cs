using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;


// Analysis and accounting attached with CreateRun or placed explicitly with wrapper configurations collect
// the same data into each definition's run result. Each theory runs both paths over equivalent boundaries.
public sealed class AttachedAndExplicitPlacementTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> IntegerProblem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    // Counting counts successful calls and their candidates; timing includes calls that fail.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CountingAndTiming_AreEqualThroughBothPaths_IncludingFailedCalls(bool attached)
    {
        var time = new ManualTimeProvider();
        IOperator<Evaluation, int> evaluator = new FlakyEvaluator(time);
        var invocations = new Counter();
        var candidates = new Counter();
        var duration = new Duration();
        var scope = ResolutionScope.Create(time);
        var placed = attached
            ? evaluator.Substitute(evaluator.Invocations.Counting(invocations), evaluator.Invocations.CountingCandidates(candidates), evaluator.Invocations.Timing(duration))
            : evaluator.Counted(invocations).Counted(candidates, OperatorCountMetric.Candidates).Timed(duration);
        var executor = Resolve(scope, placed);

        for (var call = 0; call < 4; call++)
            Record.Exception(() => executor.Evaluate([1, 2], RandomNumberGenerator.Create(1)));

        scope.Results.Result(invocations).CurrentCount.ShouldBe(2);
        scope.Results.Result(candidates).CurrentCount.ShouldBe(4);
        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(4));
    }

    // Retry around an operator sees the counting and the trace inside it through both paths, and the trace's clock
    // counts each call before the trace records it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CountingObservationAndRetry_ComposeInTheSameOrderThroughBothPaths(bool attached)
    {
        IOperator<Evaluation, int> evaluator = new FlakyEvaluator(new ManualTimeProvider());
        var evaluations = new Counter();
        var batches = evaluator.Invocations.Trace(observation => observation.Candidates.Count, [evaluations.Clock]);
        var scope = ResolutionScope.Create();
        var placed = attached
            ? evaluator.Retry(2).Substitute(batches, evaluator.Invocations.Counting(evaluations))
            : evaluator.Counted(evaluations).Observed(batches).Retry(2);
        var executor = Resolve(scope, placed);

        for (var call = 1; call <= 3; call++)
            executor.Evaluate([.. Enumerable.Range(0, call)], RandomNumberGenerator.Create(1));

        var trace = scope.Results.Result(batches);
        trace.Snapshot().Values.ShouldBe([1, 2, 3]);
        trace.By(evaluations.Clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        scope.Results.Result(evaluations).CurrentCount.ShouldBe(3);
    }

    // Both paths use the same trace, retention and clocks, so equal runs record equal entries.
    [Fact]
    public async Task Tracing_RecordsTheSameEntriesThroughBothPaths()
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var evaluations = new Counter();
        var best = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock], TraceRetention.EveryNth(2));

        var attached = algorithm.CreateRun(problem, RandomNumberGenerator.Create(7), [best, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await attached.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var placed = (algorithm with
        {
            Evaluator = algorithm.Evaluator.Counted(evaluations, OperatorCountMetric.Candidates).Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)
        }).CreateRun(problem, RandomNumberGenerator.Create(7));
        await placed.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var attachedTrace = attached.Result(best).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).ToList();
        attachedTrace.Count.ShouldBe(2);
        attached.Result(best).ShouldAllBe(point => point.Evaluations == point.At(evaluations.Clock));
        placed.Result(best).Select(point => point.Evaluations).ShouldBe(attached.Result(best).Select(point => point.Evaluations));
        placed.Result(best).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).ToList().ShouldBe(attachedTrace);
        placed.Result(evaluations).CurrentCount.ShouldBe(attached.Result(evaluations).CurrentCount);
    }

    // A shared configuration keeps one executor across a cycle's turns through both paths, and the observations of all
    // turns go into the run's one trace.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SharedExecution_KeepsOneExecutorAndOneTraceThroughBothPaths(bool attached)
    {
        var inner = new CountingExecutionAlgorithm(1, new ProblemEvaluator<int>());
        var states = inner.Iterations.Trace(observation => observation.State.Population.EvaluatedCandidates.Single().Candidate);
        IAlgorithm<int, PopulationState<int>> cycle;
        if (attached)
            cycle = (CycleAlgorithm.Create(inner) with { MaximumCycles = 3 }).Sharing(inner).Substitute(states);
        else
        {
            var observed = inner.Observed(states);
            cycle = (CycleAlgorithm.Create<int, PopulationState<int>>(observed) with { MaximumCycles = 3 }).Sharing(observed);
        }
        var run = cycle.CreateRun(IntegerProblem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        inner.ExecutionCount.ShouldBe(1);
        run.Result(states).Snapshot().Values.ShouldBe([1, 2, 3]);
    }

    // A terminator reads the same counter result as the placement that counts, whichever of the two the scope builds
    // first.
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void SeparateTermination_ReadsTheCountIndependentOfConstructionOrder(bool attached, bool terminatorFirst)
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var evaluations = new Counter();
        var terminator = new AfterOperatorCountTerminator<int>(evaluations, 2);
        var scope = ResolutionScope.Create();
        var placed = attached ? evaluator.Substitute(evaluator.Invocations.Counting(evaluations)) : evaluator.Counted(evaluations);
        var typed = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(IntegerProblem);
        var termination = terminatorFirst ? typed.Resolve(terminator) : null;
        var evaluation = typed.Resolve(placed);
        termination ??= typed.Resolve(terminator);
        var state = Population.From([0.ToEvaluated(new ObjectiveVector(0))]).ToPopulationState();

        evaluation.Evaluate([1], RandomNumberGenerator.Create(1));
        termination.IsTerminalState(state, RandomNumberGenerator.Create(1)).ShouldBeFalse();
        evaluation.Evaluate([2], RandomNumberGenerator.Create(1));
        termination.IsTerminalState(state, RandomNumberGenerator.Create(1)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BestSoFarEvaluations_DrivesATerminatorThroughBothPlacementPaths(bool attached)
    {
        var inner = new AdditiveStepAlgorithm(-1);
        var best = Analyzer.BestSoFar(inner.Evaluator, retention: TraceRetention.EveryObservation());
        var placed = attached ? inner : inner with
        {
            Evaluator = inner.Evaluator.Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)
        };
        var algorithm = (CycleAlgorithm.Create(placed) with { MaximumCycles = 20 })
            .TerminatedBy(new AfterOperatorCountTerminator<int>(best.Evaluations, 3));
        var run = algorithm.CreateRun(IntegerProblem, RandomNumberGenerator.Create(1), attached ? [best] : null);

        var state = run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        state.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(-3);
        run.Result(best.Evaluations).CurrentCount.ShouldBe(3);
        run.Result(best).Select(point => point.Evaluations).ShouldBe([1L, 2L, 3L]);
        run.Result(best).Select(point => point.Candidate).ShouldBe([-1, -2, -3]);
        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
    }

    // Reusing one configuration for several runs gives each run fresh results through both paths, including a best so
    // far and a retention that keep state across observations.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IndependentRuns_KeepTheirOwnBestSoFarAndRetentionThroughBothPaths(bool attached)
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var best = Analyzer.BestSoFar(algorithm.Evaluator);
        var everyOther = algorithm.Iterations.Trace(observation => observation.Iteration, retention: TraceRetention.EveryNth(2));
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        IAlgorithm<RealVector, PopulationState<RealVector>> configured = attached
            ? receiver.Substitute(best, everyOther)
            : (algorithm with { Evaluator = algorithm.Evaluator.Observed(best) }).Observed(everyOther);

        var first = configured.CreateRun(problem, RandomNumberGenerator.Create(3));
        await first.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var second = configured.CreateRun(problem, RandomNumberGenerator.Create(3));
        await second.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        second.Result(best).ShouldNotBeSameAs(first.Result(best));
        second.Result(best).Select(point => point.Value).ToList().ShouldBe(first.Result(best).Select(point => point.Value).ToList());
        first.Result(everyOther).Snapshot().Values.ShouldBe([2L, 4L]);
        second.Result(everyOther).Snapshot().Values.ShouldBe([2L, 4L]);
    }

    // A clock shared by two traces of one call applies once and before both observations, whichever trace is listed first.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASharedClock_TicksOnceBeforeBothObservations(bool swap)
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var candidates = new Counter();
        var first = evaluator.Invocations.Trace(observation => observation.Candidates.Count, [candidates.Clock]);
        var second = evaluator.Invocations.Trace(observation => -observation.Candidates.Count, [candidates.Clock]);
        var (outer, inner) = swap ? (second, first) : (first, second);
        var root = ResolutionScope.Create();
        var executor = Resolve(root, evaluator.Substitute(outer, inner, evaluator.Invocations.CountingCandidates(candidates)));

        executor.Evaluate([1, 2], RandomNumberGenerator.Create(1));
        executor.Evaluate([3, 4, 5], RandomNumberGenerator.Create(1));

        root.Results.Result(first).By(candidates.Clock).Select(point => point.Time).ShouldBe([2L, 5L]);
        root.Results.Result(second).By(candidates.Clock).Select(point => point.Time).ShouldBe([2L, 5L]);
        root.Results.Result(candidates).CurrentCount.ShouldBe(5);
    }

    // An operation defined outside the library declares its source and its Observed line like a built-in one,
    // and gets both paths, counters and timing from them.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnExternalOperation_IsTracedThroughBothPaths(bool attached)
    {
        IOperator<Checksum, int> checksum = new IncrementingChecksum();
        var reads = new Counter();
        var values = checksum.Invocations.Trace(observation => observation.Result, [reads.Clock]);
        var scope = ResolutionScope.Create();
        var placed = attached ? checksum.Substitute(values, checksum.Invocations.Counting(reads)) : checksum.Counted(reads).Observed(values);
        var executor = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(IntegerProblem).Resolve(placed);

        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(1);
        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(2);

        scope.Results.Result(values).By(reads.Clock).ShouldBe([new(1L, 1), new(2L, 2)]);
    }

    // An observation wrapper has one child, ChildOperator: a copy with another child observes that child through
    // the same observation wrapper.
    [Fact]
    public void AnObservedWrapper_ObservesTheChildItWraps()
    {
        var first = new IncrementingChecksum();
        var second = new IncrementingChecksum();
        var reads = first.Invocations.Trace(observation => observation.Checksum);
        var observed = first.Observed(reads) with { ChildOperator = second };
        var scope = ResolutionScope.Create();

        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(IntegerProblem).Resolve(observed)
            .Read(RandomNumberGenerator.Create(1));

        scope.Results.Result(reads).Snapshot().Values.ShouldHaveSingleItem().ShouldBeSameAs(second);
    }

    // A wrapper selected by type wraps every algorithm of the graph, the root included, so it counts the
    // resolutions of all of them.
    [Fact]
    public async Task AlgorithmWrapperSelectedByType_CountsTheResolutionsOfEveryAlgorithm()
    {
        var inner = new CountingExecutionAlgorithm(1, new ProblemEvaluator<int>());
        var resolutions = new CountAccumulator();
        IAlgorithm<int, PopulationState<int>> cycle = CycleAlgorithm.Create(inner) with { MaximumCycles = 2 };
        var run = cycle.Substitute((IAlgorithm<int, PopulationState<int>> algorithm) => new ResolutionCounting(algorithm, resolutions))
            .CreateRun(IntegerProblem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The cycle and its two turns.
        resolutions.CurrentCount.ShouldBe(3);
    }

    // Substitutions apply in order and each new one matches the original target, so later entries and later calls sit
    // closer to it and record first.
    [Fact]
    public void CreateRun_LaterEntriesAndLaterCallsSitCloser()
    {
        IOperator<Evaluation, int> evaluator = new ProblemEvaluator<int>();
        var log = new List<string>();
        ISubstitution Logging(string label) => evaluator.Invocations.Observing(_ => log.Add(label));
        var algorithm = new AdditiveStepAlgorithm(1) { Evaluator = evaluator }
            .Substitute(Logging("first"), Logging("second"))
            .Substitute(Logging("outer"));

        algorithm.CreateRun(IntegerProblem, RandomNumberGenerator.Create(1)).Complete(cancellationToken: TestContext.Current.CancellationToken);

        log.ShouldBe(["outer", "second", "first"]);
    }

    private static IEvaluationExecutor<int> Resolve(ResolutionScope scope, IOperator<Evaluation, int> evaluator) =>
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(IntegerProblem).Resolve(evaluator);

    [Fact]
    public void DurationClock_RecordsTheCompletedCallsDuration()
    {
        var time = new ManualTimeProvider();
        IOperator<Evaluation, int> evaluator = new FlakyEvaluator(time);
        var duration = new Duration();
        var trace = evaluator.Invocations.Trace(observation => observation.Candidates.Count, [duration.Clock]);
        var scope = ResolutionScope.Create(time);
        var executor = Resolve(scope, evaluator.Substitute(trace, evaluator.Invocations.Timing(duration)));

        Should.Throw<RetryableOperationException>(() => executor.Evaluate([1], RandomNumberGenerator.Create(1)));
        executor.Evaluate([1, 2], RandomNumberGenerator.Create(1));
        time.Advance(TimeSpan.FromSeconds(100));
        Should.Throw<RetryableOperationException>(() => executor.Evaluate([1], RandomNumberGenerator.Create(1)));
        executor.Evaluate([1, 2, 3], RandomNumberGenerator.Create(1));

        var result = scope.Results.Result(trace);
        result.Select(entry => entry.At(duration.Clock)).ShouldBe([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)]);
        result.By(duration.Clock).ShouldBe([new(TimeSpan.FromSeconds(2), 2), new(TimeSpan.FromSeconds(4), 3)]);
        scope.Results.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(4));
        Should.Throw<InvalidOperationException>(() => result.First().At(Clock.Elapsed)).Message.ShouldBe("The trace does not use the requested clock.");
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem, int maximumGenerations) =>
        new()
        {
            PopulationSize = 8,
            MaximumGenerations = maximumGenerations,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(0.3, 0.2),
            Selector = TournamentSelector.For(problem, 2),
            Elites = 1
        };

    // Spends one second in each call and fails every first attempt with a retryable exception.
    private sealed record FlakyEvaluator(ManualTimeProvider Time) : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope) =>
            new Executor(Time, scope.Problem);

        private sealed class Executor(ManualTimeProvider time, IProblem<int, DummySearchSpace<int>> problem) : EvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(problem)
        {
            private int calls;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                time.Advance(TimeSpan.FromSeconds(1));
                if (++calls % 2 == 1)
                    throw new RetryableOperationException("First attempt failed.");
                return [.. candidates.Select(candidate => new ObjectiveVector(candidate))];
            }
        }
    }

    // Returns 1, 2, 3, ... on its calls.
    private sealed record IncrementingChecksum : IOperator<Checksum, int>
    {
        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor();

        private sealed class Executor : IChecksumExecutor
        {
            private int value;

            public int Read(IRandomNumberGenerator random) => ++value;
        }
    }

    // Counts the executors resolved for the algorithm it wraps, written outside the library.
    private sealed record ResolutionCounting(IAlgorithm<int, PopulationState<int>> ChildAlgorithm, CountAccumulator Resolutions)
        : WrappingAlgorithm<int, PopulationState<int>>(ChildAlgorithm)
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            Resolutions.IncrementBy(1);
            return scope.Resolve(ChildAlgorithm);
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
