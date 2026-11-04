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

// An instrument attached with advice and the same instrument placed explicitly with a wrapper configuration collect
// the same data into the run's one result. Each theory runs both paths over equivalent boundaries.
public sealed class InstrumentPlacementTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> IntegerProblem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    // Counting counts successful calls and their candidates; timing includes calls that fail.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CountingAndTiming_AreEqualThroughBothPaths_IncludingFailedCalls(bool attached)
    {
        var time = new ManualTimeProvider();
        var evaluator = new FlakyEvaluator(time);
        var invocations = evaluator.Invocations.Count();
        var candidates = evaluator.Invocations.CountCandidates();
        var duration = evaluator.Invocations.Duration(time);
        var scope = ResolutionScope.Create();
        IOperator<Evaluation, int> placed = evaluator;
        if (attached)
            Attach(scope, invocations, candidates, duration);
        else
            placed = evaluator.Counted(invocations).Counted(candidates).Timed(duration);
        var executor = Resolve(scope, placed);

        for (var call = 0; call < 4; call++)
            Record.Exception(() => executor.Evaluate([1, 2], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem));

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
        var evaluator = new FlakyEvaluator(new ManualTimeProvider());
        var evaluations = evaluator.Invocations.Count();
        var batches = evaluator.Invocations.Trace(observation => observation.Candidates.Count, [evaluations]);
        var scope = ResolutionScope.Create();
        IOperator<Evaluation, int> placed;
        if (attached)
        {
            Attach(scope, batches);
            placed = evaluator.Retry(2);
        }
        else
            placed = evaluator.Counted(evaluations).Observed(batches).Retry(2);
        var executor = Resolve(scope, placed);

        for (var call = 1; call <= 3; call++)
            executor.Evaluate([.. Enumerable.Range(0, call)], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);

        var trace = scope.Results.Result(batches);
        trace.Snapshot().Values.ShouldBe([1, 2, 3]);
        trace.By(evaluations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        scope.Results.Result(evaluations).CurrentCount.ShouldBe(3);
    }

    // Both paths use the same trace, retention and clocks, so equal runs record equal entries.
    [Fact]
    public async Task Tracing_RecordsTheSameEntriesThroughBothPaths()
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar([evaluations], TraceRetention.EveryNth(2));

        var attached = algorithm.Advised(best).CreateRun(problem, RandomNumberGenerator.Create(7));
        await attached.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var placed = (algorithm with { Evaluator = algorithm.Evaluator.Counted(evaluations).Observed(best) }).CreateRun(problem, RandomNumberGenerator.Create(7));
        await placed.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var attachedTrace = attached.Result(best).By(evaluations);
        attachedTrace.Count.ShouldBe(2);
        placed.Result(best).By(evaluations).ShouldBe(attachedTrace);
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
            cycle = (CycleAlgorithm.Create(inner) with { MaximumCycles = 3 }).Sharing(inner).Advised(states);
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
        var evaluator = new ProblemEvaluator<int>();
        var evaluations = evaluator.Invocations.Count();
        var terminator = new AfterOperatorCountTerminator<int>(evaluations, 2);
        var scope = ResolutionScope.Create();
        if (attached)
            Attach(scope, evaluations);
        IOperator<Evaluation, int> placed = attached ? evaluator : evaluator.Counted(evaluations);
        var typed = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>();
        var termination = terminatorFirst ? typed.Resolve(terminator) : null;
        var evaluation = typed.Resolve(placed);
        termination ??= typed.Resolve(terminator);
        var state = Population.From([0.ToEvaluated(new ObjectiveVector(0))]).ToPopulationState();

        evaluation.Evaluate([1], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);
        termination.IsTerminalState(state, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem).ShouldBeFalse();
        evaluation.Evaluate([2], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);
        termination.IsTerminalState(state, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem).ShouldBeTrue();
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
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar();
        var everyOther = algorithm.Iterations.Trace(observation => observation.Iteration, retention: TraceRetention.EveryNth(2));
        IAlgorithm<RealVector, PopulationState<RealVector>> configured = attached
            ? algorithm.Advised(best, everyOther)
            : (algorithm with { Evaluator = algorithm.Evaluator.Observed(best) }).Observed(everyOther);

        var first = configured.CreateRun(problem, RandomNumberGenerator.Create(3));
        await first.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var second = configured.CreateRun(problem, RandomNumberGenerator.Create(3));
        await second.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        second.Result(best).ShouldNotBeSameAs(first.Result(best));
        second.Result(best).Snapshot().Values.ShouldBe(first.Result(best).Snapshot().Values);
        first.Result(everyOther).Snapshot().Values.ShouldBe([2L, 4L]);
        second.Result(everyOther).Snapshot().Values.ShouldBe([2L, 4L]);
    }

    // A clock shared by two traces applies once and before both observations, whichever trace is attached first and
    // whether they are attached in one scope or in a scope and its child.
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void ASharedClock_TicksOnceBeforeBothObservations(bool sameScope, bool swap)
    {
        var evaluator = new ProblemEvaluator<int>();
        var candidates = evaluator.Invocations.CountCandidates();
        var first = evaluator.Invocations.Trace(observation => observation.Candidates.Count, [candidates]);
        var second = evaluator.Invocations.Trace(observation => -observation.Candidates.Count, [candidates]);
        var (outer, inner) = swap ? (second, first) : (first, second);
        var root = ResolutionScope.Create();
        Attach(root, outer);
        var scope = sameScope ? root : root.CreateChildScope();
        Attach(scope, inner);
        var executor = Resolve(scope, evaluator);

        executor.Evaluate([1, 2], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);
        executor.Evaluate([3, 4, 5], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);

        root.Results.Result(first).By(candidates).Select(point => point.Time).ShouldBe([2L, 5L]);
        root.Results.Result(second).By(candidates).Select(point => point.Time).ShouldBe([2L, 5L]);
        root.Results.Result(candidates).CurrentCount.ShouldBe(5);
    }

    // An operation defined outside the library declares its source and its Observed line like a built-in one,
    // and gets both paths, counters and timing from them.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnExternalOperation_IsTracedThroughBothPaths(bool attached)
    {
        var checksum = new IncrementingChecksum();
        var reads = checksum.Invocations.Count();
        var values = checksum.Invocations.Trace(observation => observation.Result, [reads]);
        var scope = ResolutionScope.Create();
        IOperator<Checksum, int> placed = checksum;
        if (attached)
            Attach(scope, values);
        else
            placed = checksum.Counted(reads).Observed(values);
        var executor = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(placed);

        executor.Read(RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem).ShouldBe(1);
        executor.Read(RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem).ShouldBe(2);

        scope.Results.Result(values).By(reads).ShouldBe([new(1L, 1), new(2L, 2)]);
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

        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(observed)
            .Read(RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, IntegerProblem);

        scope.Results.Result(reads).Snapshot().Values.ShouldHaveSingleItem().ShouldBeSameAs(second);
    }

    // Advice an Advised node adds never applies to that node itself, so an instrument selecting every algorithm
    // can be attached at a run's root and counts the algorithms below it.
    [Fact]
    public async Task Advised_DoesNotSelectItself()
    {
        var inner = new CountingExecutionAlgorithm(1, new ProblemEvaluator<int>());
        var resolutions = new AllAlgorithmResolutions();
        var run = (CycleAlgorithm.Create(inner) with { MaximumCycles = 2 }).Advised(resolutions).CreateRun(IntegerProblem, RandomNumberGenerator.Create(1));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The cycle and its two turns; the Advised node at the root is not among them.
        run.Result(resolutions).CurrentCount.ShouldBe(3);
    }

    // The exclusion covers only the node adding the advice: an inner Advised node attaching an instrument that
    // selects the outer one, which is already resolved, is an error.
    [Fact]
    public void Advised_AddingAdviceForAnotherResolvedNode_IsAnError()
    {
        var inner = new CountingExecutionAlgorithm(1, new ProblemEvaluator<int>());
        var algorithm = inner.Advised(new AllAlgorithmResolutions()).Advised(inner.Calls.Count());

        Should.Throw<InvalidOperationException>(() => algorithm.CreateRun(IntegerProblem, RandomNumberGenerator.Create(1)).Complete(cancellationToken: TestContext.Current.CancellationToken))
            .Message.ShouldContain("already resolved without it");
    }

    // The first instrument listed wraps closest, and nesting keeps the order, so inner advice wraps closer.
    [Fact]
    public void Advised_FirstListedAndInnerAttachmentsWrapClosest()
    {
        var evaluator = new ProblemEvaluator<int>();
        var log = new List<string>();
        var algorithm = new AdditiveStepAlgorithm(1) { Evaluator = evaluator }
            .Advised(new Logging(evaluator, "first", log), new Logging(evaluator, "second", log))
            .Advised(new Logging(evaluator, "outer", log));

        algorithm.CreateRun(IntegerProblem, RandomNumberGenerator.Create(1)).Complete(cancellationToken: TestContext.Current.CancellationToken);

        log.ShouldBe(["first", "second", "outer"]);
    }

    private static void Attach(ResolutionScope scope, params Instrument[] instruments)
    {
        foreach (var instrument in instruments)
            scope.AddAspect(scope.Results.Aspect(instrument));
    }

    private static IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Resolve(ResolutionScope scope, IOperator<Evaluation, int> evaluator) =>
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(evaluator);

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
        protected override IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
            new Executor(Time);

        private sealed class Executor(ManualTimeProvider time) : EvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            private int calls;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random,
                DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
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
        IExecutor IOperator<Checksum, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor();

        private sealed class Executor : IChecksumExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
        {
            private int value;

            public int Read(IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => ++value;
        }
    }

    // A counter of the executors resolved for every algorithm, written outside the library.
    private sealed class AllAlgorithmResolutions : Counter
    {
        protected override Aspect CreateAspect(CountAccumulator result, RunResults results) =>
            Aspect.Create(Pointcut.OfType<IAlgorithm<int, PopulationState<int>>>(), new ResolutionCounting(result));

        private sealed class ResolutionCounting(CountAccumulator resolutions) : AlgorithmAdvice<int, PopulationState<int>>
        {
            public override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> Wrap<TSearchSpace, TProblem>(
                IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> executor, IAlgorithm<int, PopulationState<int>> source)
            {
                resolutions.IncrementBy(1);
                return executor;
            }
        }
    }

    // Logs its label after each evaluation, so the order of the log is the order of the wrappers from the inside out.
    private sealed class Logging(IOperator<Evaluation, int> evaluator, string label, List<string> log) : Instrument<List<string>>
    {
        protected override List<string> CreateResult(RunResults results) => log;

        protected override Aspect CreateAspect(List<string> result, RunResults results) =>
            Aspect.Observe(evaluator.Invocations, _ => result.Add(label));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref timestamp);

        public void Advance(TimeSpan by) => Interlocked.Add(ref timestamp, by.Ticks);
    }
}
