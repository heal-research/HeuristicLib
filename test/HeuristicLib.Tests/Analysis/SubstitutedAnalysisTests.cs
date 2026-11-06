using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using static HEAL.HeuristicLib.Tests.Execution.SubstitutionFixture;
using GaussianMutator = HEAL.HeuristicLib.Encodings.RealVectors.GaussianMutator;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

// Instruments placed through their substitutions, against the scenarios of plans/operator-unification-system-spec.md and the
// evidence table of plans/aspects-as-automatic-composition.md. A theory runs both paths where the old one exists.
public class SubstitutedAnalysisTests
{
    [Fact]
    public void Trace_EqualsTheGraphWrittenByHand()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 1, m, box);
        var clock = m.Invocations.Count();
        var trace = m.Invocations.Trace(_ => 1, [clock]);

        a.Substitute(trace.Substitutions).ShouldBe(a with { Mutator = m.Counted(clock).Observed(trace) });
        m.Observed(trace).ShouldBe(m.Observed(trace));
    }

    // Aspects table, observation changes nothing: a seeded run yields the same states with and without analyzers.
    [Fact]
    public void Analyzers_DoNotChangeASeededRun()
    {
        var problem = new TestFunctionProblem(new SphereFunction(3));
        var ga = RealGa(problem) with { MaximumGenerations = 5 };
        var evaluations = ga.Evaluator.Invocations.CountCandidates();
        var best = ga.Evaluator.Invocations.TraceBestSoFar([evaluations]);
        var mutations = ga.Mutator.Invocations.Trace(_ => 1);

        var observed = ga.Substitute([.. best.Substitutions, .. mutations.Substitutions]);

        observed.ShouldNotBe(ga);
        States(observed, problem).ShouldBe(States(ga, problem));
    }

    // S8 and the aspects table, deferred construction: one trace records every selected call across fresh executors, and
    // each run has its own result.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Trace_RecordsEveryCallAcrossFreshExecutorsAndPerRun(bool attached)
    {
        var (box, m) = NewMutator();
        var trace = m.Invocations.Trace(_ => 1);
        var cycle = Place(Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box)), trace, attached);

        var first = cycle.CreateRun(Problem, Rng);
        first.Complete(cancellationToken: Ct);
        var second = cycle.CreateRun(Problem, Rng);
        second.Complete(cancellationToken: Ct);

        box.Builds.ShouldBe(8);
        first.Result(trace).SampleCount.ShouldBe(10);
        second.Result(trace).SampleCount.ShouldBe(10);
    }

    // Q2: only A's calls, whether the substitution runs on A or the wrapper is written in A's slot.
    [Fact]
    public void TraceOnOneCaller_SeesOnlyThatCallersCallsOfTheSharedConfiguration()
    {
        var (box, m) = NewMutator();
        var onA = m.Invocations.Trace(_ => 1);
        var inSlot = m.Invocations.Trace(_ => 1);
        var a = NewCaller("A", 3, 1, m, box);
        var b = NewCaller("B", 2, 1, m, box);

        var first = (CycleAlgorithm.Create<int, PopulationState<int>>(a.Substitute(onA.Substitutions), b) with { MaximumCycles = 1 }).CreateRun(Problem, Rng);
        first.Complete(cancellationToken: Ct);
        var second = (CycleAlgorithm.Create<int, PopulationState<int>>(a with { Mutator = m.Observed(inSlot) }, b) with { MaximumCycles = 1 }).CreateRun(Problem, Rng);
        second.Complete(cancellationToken: Ct);

        first.Result(onA).SampleCount.ShouldBe(3);
        second.Result(inSlot).SampleCount.ShouldBe(3);
    }

    // Q3: a wrapper is an algorithm like every other node, as a selection of every algorithm shows.
    [Fact]
    public void CountingEveryAlgorithmByType_CountsWrappersLikeEveryOtherNode()
    {
        var (box, m) = NewMutator();
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 1, 1, m, box);
        IAlgorithm<int, PopulationState<int>> b = NewCaller("B", 1, 1, m, box);
        IAlgorithm<int, PopulationState<int>> named = a.Named("A");

        CallsOfEveryAlgorithm(a).ShouldBe(1);
        CallsOfEveryAlgorithm(named).ShouldBe(2);
        CallsOfEveryAlgorithm(Cycle(a, b)).ShouldBe(5);
        CallsOfEveryAlgorithm(Cycle(named, b)).ShouldBe(7);

        static long CallsOfEveryAlgorithm(IAlgorithm<int, PopulationState<int>> root)
        {
            var calls = new Counter();
            var counted = root.Substitute(Substitution.Of<IAlgorithm<int, PopulationState<int>>>(
                node => new CountedAlgorithm<int, PopulationState<int>, AlgorithmCallObservation<int, PopulationState<int>>>(node, calls)));
            var run = counted.CreateRun(Problem, Rng);
            run.Complete(cancellationToken: Ct);
            return run.Result(calls).CurrentCount;
        }
    }

    // Q4: one substitution applies once, two count separately, and a wrapper written twice does what is written.
    [Fact]
    public void AttachingCountingTwice_FollowsTheSubstitutionsThatWereWritten()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 1, m, box);

        var once = new Counter();
        var substitution = Substitution.Of(m, node => node.Counted(once));
        var twiceListed = a.Substitute(substitution, substitution).CreateRun(Problem, Rng);
        twiceListed.Complete(cancellationToken: Ct);
        twiceListed.Result(once).CurrentCount.ShouldBe(3);

        var (first, second) = (new Counter(), new Counter());
        var separate = a.Substitute(Substitution.Of(m, node => node.Counted(first)), Substitution.Of(m, node => node.Counted(second))).CreateRun(Problem, Rng);
        separate.Complete(cancellationToken: Ct);
        separate.Result(first).CurrentCount.ShouldBe(3);
        separate.Result(second).CurrentCount.ShouldBe(3);

        var written = new Counter();
        var writtenTwice = (a with { Mutator = m.Counted(written).Counted(written) }).CreateRun(Problem, Rng);
        writtenTwice.Complete(cancellationToken: Ct);
        writtenTwice.Result(written).CurrentCount.ShouldBe(6);

        // Case 3 of the spec, a target this graph does not hold, is an error rather than a count of 0.
        var (_, other) = NewMutator();
        Should.Throw<InvalidOperationException>(() => a.Substitute(Substitution.Of(other, node => node.Counted(once))));
    }

    // Q5: a trace inside A's turns continues across turns as one result.
    [Fact]
    public void TraceInsideACycleChild_ContinuesAcrossTurns()
    {
        var (box, m) = NewMutator();
        var trace = m.Invocations.Trace(_ => 1);
        var a = NewCaller("A", 3, 1, m, box).Substitute(trace.Substitutions);

        var run = Cycle(a, NewCaller("B", 2, 1, m, box)).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(trace).SampleCount.ShouldBe(6);
    }

    // Q6: a failure in the middle of a run surfaces, ends the run, and leaves results readable.
    [Fact]
    public void ConstructionFailureInALaterTurn_EndsTheRunAndLeavesResultsReadable()
    {
        var (box, m) = NewMutator();
        var trace = m.Invocations.Trace(_ => 1);
        var cycle = Cycle(NewCaller("A", 3, 1, m, box), new BrokenAlgorithm());
        var counted = cycle.Substitute(trace.Substitutions).Counting(m, out var calls);

        var run = counted.CreateRun(Problem, Rng);
        run.LifecycleState.ShouldBe(RunLifecycleState.Ready);
        Should.Throw<InvalidOperationException>(() => run.Complete(cancellationToken: Ct)).Message.ShouldBe("broken");

        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        Should.Throw<InvalidOperationException>(() => run.Complete(cancellationToken: Ct));
        run.Result(trace).SampleCount.ShouldBe(3);
        run.Result(calls).CurrentCount.ShouldBe(3);
    }

    // Aspects table, counting: both paths measure equally, including a failed attempt that is not counted.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Counting_MeasuresTheSameThroughBothPaths(bool attached)
    {
        var box = new Recording();
        IOperator<Evaluation, int> flaky = new M(box) { FailFirstCall = true };
        var retried = flaky.Retry(2);
        var counter = flaky.Invocations.Count();
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 3, 1, retried, box);

        var placed = attached ? a.Advised(counter) : a.Substitute(counter.Substitutions);
        var run = placed.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        box.Calls.ShouldBe(4);
        run.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void Timing_EqualsTheGraphWrittenByHand()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 1, m, box);
        var duration = m.Invocations.Duration();

        a.Substitute(duration.Substitutions).ShouldBe(a with { Mutator = m.Timed(duration) });
    }

    // Aspects table, ordering: counting, observation and retry give the same sequence through both paths.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CountingObservationAndRetry_GiveTheSameSequenceThroughBothPaths(bool attached)
    {
        var box = new Recording();
        IOperator<Evaluation, int> flaky = new M(box) { FailFirstCall = true };
        var evaluations = flaky.Invocations.Count();
        var batches = flaky.Invocations.Trace(observation => observation.Candidates.Count, [evaluations]);
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 3, 1, flaky.Retry(2), box);

        var run = (attached ? a.Advised(batches) : a.Substitute(batches.Substitutions)).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(batches).Snapshot().Values.ShouldBe([1, 1, 1]);
        run.Result(batches).By(evaluations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    // Aspects table, tracing: same value functions, retention and clocks, so equal seeded runs record equal entries.
    [Fact]
    public async Task Tracing_RecordsTheSameEntriesThroughBothPaths()
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var ga = RealGa(problem) with { MaximumGenerations = 4 };
        var evaluations = ga.Evaluator.Invocations.CountCandidates();
        var best = ga.Evaluator.Invocations.TraceBestSoFar([evaluations], TraceRetention.EveryNth(2));

        var attached = ga.Advised(best).CreateRun(problem, RandomNumberGenerator.Create(7));
        await attached.CompleteAsync(cancellationToken: Ct);
        var placed = ga.Substitute(best.Substitutions).CreateRun(problem, RandomNumberGenerator.Create(7));
        await placed.CompleteAsync(cancellationToken: Ct);

        attached.Result(best).By(evaluations).Count.ShouldBeGreaterThan(0);
        placed.Result(best).By(evaluations).ShouldBe(attached.Result(best).By(evaluations));
        placed.Result(evaluations).CurrentCount.ShouldBe(attached.Result(evaluations).CurrentCount);
    }

    // Aspects table, internal progress: the start and end of each inner call, including a turn that yields no state.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheCallsOfInnerAlgorithms_AreRecordedThroughBothPaths(bool attached)
    {
        var (box, m) = NewMutator();
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 1, 1, m, box);
        IAlgorithm<int, PopulationState<int>> idle = NewCaller("B", 1, 0, m, box);
        var calls = Analyzer.Trace<AlgorithmCallObservation<int, PopulationState<int>>, AlgorithmCallEvent>([a.Calls, idle.Calls], observation => observation.Event);
        IAlgorithm<int, PopulationState<int>> cycle = Cycle(a, idle) with { MaximumCycles = 1 };

        var run = (attached ? cycle.Advised(calls) : cycle.Substitute(calls.Substitutions)).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(calls).Snapshot().Values.ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed, AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed]);
    }

    // Aspects table, shared execution: one executor across turns, with each requester's own wrapper around it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Trace_RecordsEveryCallOfASharedConfiguration(bool attached)
    {
        var (box, m) = NewMutator();
        var trace = m.Invocations.Trace(_ => 1);
        var shared = Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box)).Sharing(m);

        var run = Place(shared, trace, attached).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        box.Builds.ShouldBe(1);
        run.Result(trace).SampleCount.ShouldBe(10);
    }

    // Aspects table, separate termination: a counter and a terminator share the run's result whichever is built first.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnInstrumentCounter_ReachesASeparateTerminator(bool attached)
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var ga = RealGa(problem) with { MaximumGenerations = 50 };
        var mutations = ga.Mutator.Invocations.Count();

        var terminated = ga with { Terminator = new AfterOperatorCountTerminator<RealVector>(mutations, 3) };
        var run = Place(terminated, mutations, attached).CreateRun(problem, RandomNumberGenerator.Create(42));
        var states = run.Stream(cancellationToken: Ct).Count();

        states.ShouldBeLessThan(50);
        run.Result(mutations).CurrentCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    // Aspects table, independent runs: state of a value function and retention belong to the run.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OneConfiguration_GivesIndependentResultsPerRun(bool attached)
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var ga = RealGa(problem) with { MaximumGenerations = 3 };
        var best = ga.Evaluator.Invocations.TraceBestSoFar();
        var placed = Place(ga, best, attached);

        var first = placed.CreateRun(problem, RandomNumberGenerator.Create(1));
        var second = placed.CreateRun(problem, RandomNumberGenerator.Create(1));
        await first.CompleteAsync(cancellationToken: Ct);
        await second.CompleteAsync(cancellationToken: Ct);

        first.Result(best).ShouldNotBeSameAs(second.Result(best));
        second.Result(best).Snapshot().Values.ShouldBe(first.Result(best).Snapshot().Values);
    }

    // Aspects table, shared clocks: in one call the clock ticks once, closest to the node, in either order of the traces.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TwoTracesSharingACounter_TickItOnceBeforeBothObservations(bool firstThenSecond)
    {
        var (box, m) = NewMutator();
        var clock = m.Invocations.Count();
        var first = m.Invocations.Trace(_ => 1, [clock]);
        var second = m.Invocations.Trace(_ => 2, [clock]);
        var a = NewCaller("A", 3, 1, m, box);

        IReadOnlyList<Substitution> substitutions = firstThenSecond
            ? [.. first.Substitutions, .. second.Substitutions]
            : [.. second.Substitutions, .. first.Substitutions];
        var run = a.Substitute(substitutions).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(clock).CurrentCount.ShouldBe(3);
        run.Result(first).By(clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        run.Result(second).By(clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    // The hard case of a custom clock that observes: a consumer places it the same way.
    [Fact]
    public void TwoTracesSharingACustomClockThatObserves_TickItOnceBeforeBothObservations()
    {
        var (box, m) = NewMutator();
        var clock = new EvaluationCallClock(m);
        var first = m.Invocations.Trace(_ => 1, [clock]);
        var second = m.Invocations.Trace(_ => 2, [clock]);

        var run = NewCaller("A", 3, 1, m, box).Substitute([.. first.Substitutions, .. second.Substitutions]).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(clock).CurrentCount.ShouldBe(3);
        run.Result(first).By(clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        run.Result(second).By(clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    // D36, the known gap: separate calls wrap the clock twice, so the clock ticks twice per call.
    [Fact]
    public void TwoTracesSharingAClockInSeparateCalls_TickItTwice()
    {
        var (box, m) = NewMutator();
        var clock = m.Invocations.Count();
        var first = m.Invocations.Trace(_ => 1, [clock]);
        var second = m.Invocations.Trace(_ => 2, [clock]);

        var run = NewCaller("A", 3, 1, m, box).Substitute(first.Substitutions).Substitute(second.Substitutions).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(clock).CurrentCount.ShouldBe(6);
    }

    // The new counter is a clock too. Naming the wrapper keeps the entry after the count; naming the operator inside it
    // records before the count, because the observation then sits below the wrapper.
    [Fact]
    public void ANewCounterAsAClock_ReadsWhatTheTargetOfTheTraceImplies()
    {
        var (box, m) = NewMutator();
        var counted = NewCaller("A", 3, 1, m, box).Counting(m, out var calls);
        var aroundTheCount = counted.Mutator.Invocations.Trace(_ => 1, [calls]);
        var insideTheCount = m.Invocations.Trace(_ => 1, [calls]);

        var around = counted.Substitute(aroundTheCount.Substitutions).CreateRun(Problem, Rng);
        around.Complete(cancellationToken: Ct);
        var inside = counted.Substitute(insideTheCount.Substitutions).CreateRun(Problem, Rng);
        inside.Complete(cancellationToken: Ct);

        around.Result(aroundTheCount).By(calls).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        inside.Result(insideTheCount).By(calls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    // Aspects table, consumer extensions: an operation defined outside the library, through both paths.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnExternalOperation_IsCountedAndTracedThroughBothPaths(bool attached)
    {
        var checksum = new IncrementingChecksum();
        var reads = checksum.Invocations.Count();
        var values = checksum.Invocations.Trace(observation => observation.Result, [reads]);
        var scope = ResolutionScope.Create();
        IOperator<Checksum, int> placed = checksum;
        if (attached)
            scope.AddAspect(scope.Results.Aspect(values));
        else
            placed = placed.Substitute(values.Substitutions);
        var executor = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(placed);

        executor.Read(Rng, DummySearchSpace<int>.Instance, Problem).ShouldBe(1);
        executor.Read(Rng, DummySearchSpace<int>.Instance, Problem).ShouldBe(2);

        scope.Results.Result(values).By(reads).ShouldBe([new(1L, 1), new(2L, 2)]);
    }

    [Fact]
    public void AnExternalOperation_IsCountedWithTheNewCounter()
    {
        IOperator<Checksum, int> checksum = new IncrementingChecksum();
        var counted = checksum.Counting(checksum, out var reads);
        var scope = ResolutionScope.Create();
        var executor = scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(counted);

        executor.Read(Rng, DummySearchSpace<int>.Instance, Problem);

        scope.Results.Result(reads).CurrentCount.ShouldBe(1);
    }

    // The receiver's own iterations need a receiver typed by the interface, which the shortcut returns.
    [Fact]
    public void AnAnalyzerOnTheReceiversOwnIterations_NeedsAnInterfaceTypedReceiver()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 1, 3, m, box);
        IAlgorithm<int, PopulationState<int>> receiver = a;
        var states = receiver.Iterations.Trace(observation => observation.Iteration);

        Should.Throw<InvalidOperationException>(() => a.Substitute(states.Substitutions));

        var run = receiver.Substitute(states.Substitutions).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);
        run.Result(states).Snapshot().Values.ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public void CountingAnAlgorithmBoundary_CountsStatesOrStartedCalls()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 1, 3, m, box);

        var counted = a.Counting(a.Iterations, out var iterations).Counting(m, out var mutations);
        var run = counted.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);
        run.Result(iterations).CurrentCount.ShouldBe(3);
        run.Result(mutations).CurrentCount.ShouldBe(3);

        var byCalls = a.Counting(a.Calls, out var calls).CreateRun(Problem, Rng);
        byCalls.Complete(cancellationToken: Ct);
        byCalls.Result(calls).CurrentCount.ShouldBe(1);
    }

    // Applying a stable substitution twice counts once, which needs the same instances however the first reads race.
    [Fact]
    public void Substitutions_AreTheSameInstancesUnderConcurrentFirstReads()
    {
        var (_, m) = NewMutator();
        for (var round = 0; round < 200; round++)
        {
            var trace = m.Invocations.Trace(_ => 1);
            var reads = new IReadOnlyList<Substitution>[8];

            Parallel.For(0, reads.Length, index => reads[index] = trace.Substitutions);

            reads.ShouldAllBe(read => ReferenceEquals(read, reads[0]));
        }
    }

    // Separate calls cannot name the receiver after they copied it, and one call can.
    [Fact]
    public void OneCall_CountsAnOperatorAndTheReceiversBoundary()
    {
        var (box, m) = NewMutator();
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 1, 3, m, box);

        Should.Throw<InvalidOperationException>(() => a.Counting(m, out _).Counting(a.Iterations, out _));

        var run = a.Substitute(Substitution.Counting(m, out var mutations), Substitution.Counting(a.Iterations, out var iterations)).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(mutations).CurrentCount.ShouldBe(3);
        run.Result(iterations).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CountingAnInnerAlgorithmBoundary_SpansItsTurns()
    {
        var (box, m) = NewMutator();
        IAlgorithm<int, PopulationState<int>> a = NewCaller("A", 1, 2, m, box);
        var cycle = Cycle(a, NewCaller("B", 1, 1, m, box));

        var run = cycle.Counting(a.Iterations, out var iterations).CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(iterations).CurrentCount.ShouldBe(4);
    }

    // A source that only advice can use says so instead of placing nothing.
    [Fact]
    public void ASourceFromAnAdviceFactory_ReportsThatItCannotBePlaced()
    {
        var (_, m) = NewMutator();
        var source = ObservationSource.Create(m, (Action<int> _) => new NoAdvice());

        Should.Throw<InvalidOperationException>(() => source.Trace(value => value).Substitutions).Message.ShouldContain("Advised");
    }

    private static IAlgorithm<TCandidate, TSearchState> Place<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, Instrument instrument, bool attached)
        where TSearchState : class, ISearchState =>
        attached ? algorithm.Advised(instrument) : algorithm.Substitute(instrument.Substitutions);

    private static List<string> States(IAlgorithm<RealVector, PopulationState<RealVector>> algorithm, TestFunctionProblem problem) =>
        [.. Enumerable.Select(algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: Ct),
            static state => string.Join(";", state.Population.Select(static member => $"{member.Candidate}|{member.ObjectiveVector}")))];

    private static GeneticAlgorithm<RealVector> RealGa(TestFunctionProblem problem) => new()
    {
        PopulationSize = 5,
        Creator = new UniformDistributedCreator(problem.SearchSpace),
        Crossover = new SinglePointCrossover(),
        Mutator = new GaussianMutator(0.1, 0.1),
        MutationRate = 1.0,
        Selector = RandomSelector.For(problem),
        Elites = 0
    };

    private sealed class EvaluationCallClock(IOperator<Evaluation, int> evaluator) : Clock<long, CountAccumulator>, IObservationInstrument<EvaluationObservation<int>>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [evaluator.Invocations.RecordingInto(this)];

        public Action<EvaluationObservation<int>> CreateRecorder(RunResults results)
        {
            var count = results.Result(this);
            return _ => count.IncrementBy(1);
        }

        protected override CountAccumulator CreateResult(RunResults results) => new();

        protected override long Read(CountAccumulator result) => result.CurrentCount;
    }

    private sealed record BrokenAlgorithm : Algorithm<BrokenAlgorithm, int, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            throw new InvalidOperationException("broken");
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

    private sealed class NoAdvice : OperatorAdvice<Evaluation, int>
    {
        public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation, IOperator<Evaluation, int> source) => executor;
    }
}
