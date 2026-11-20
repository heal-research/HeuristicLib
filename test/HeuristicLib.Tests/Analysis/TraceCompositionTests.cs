using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers composing a trace from an observation source and a function of each observation.
/// </summary>
public class TraceCompositionTests
{
    [Fact]
    public void TypedClock_MixedAndVariantRequestsShareTheRootsReading()
    {
        var counter = new Counter();
        var clock = counter.Clock;
        IClock mixed = clock;
        var scope = ResolutionScope.Create();

        var reading = scope.Results.Result(mixed);
        scope.Results.Result<IClockReading<long>>(clock).ShouldBeSameAs(reading);
        scope.Results.Result<IRunResult>(clock).ShouldBeSameAs(reading);
        scope.Results.Result(counter).IncrementBy(3);

        reading.Read().ShouldBe(3L);
        scope.Results.Result<IClockReading<long>>(clock).Read().ShouldBe(3L);
    }

    [Fact]
    public void TraceAnalyzer_RejectsAnEmptySourceList()
    {
        IReadOnlyList<ObservationSource<EvaluationObservation<int>>> sources = [];
        Should.Throw<InvalidOperationException>(() => Analyzer.Trace(sources, observation => observation.Candidates.Count))
            .Message.ShouldBe("A trace analyzer requires at least one observation source.");
    }

    [Fact]
    public async Task AnAnalyzerRepeatedInOneCall_IsPlacedOnce_AndAnotherCallPlacesItAgain()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.Iterations.TracePopulationQuality();
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var run = receiver.Substitute(trace, trace).CreateRun(problem, RandomNumberGenerator.Create(42), [trace]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(trace).SampleCount.ShouldBe(2);
    }

    [Fact]
    public void ResolutionFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1) with { Creator = new FailingCreator() };
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [trace]);
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        run.Result(trace).Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task EarlyStreamDisposal_PreservesTheRunsTraceWhileAnotherRunHasItsOwn()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.Iterations.TracePopulationQuality();
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var analyzed = receiver.Substitute(trace);
        var run = analyzed.CreateRun(problem, RandomNumberGenerator.Create(42));
        await using (var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                     .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await stream.MoveNextAsync()).ShouldBeTrue();
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        run.Result(trace).SampleCount.ShouldBe(1);
        var another = analyzed.CreateRun(problem, RandomNumberGenerator.Create(43));
        await another.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        another.Result(trace).SampleCount.ShouldBe(3);
        run.Result(trace).SampleCount.ShouldBe(1);
    }

    [Fact]
    public async Task Cancellation_PausesTheRunAndPreservesCollectedSamples()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.Iterations.TracePopulationQuality();
        using var cancellation = new CancellationTokenSource();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [trace]);
        await using var stream = run.Stream(cancellationToken: cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        (await stream.MoveNextAsync()).ShouldBeTrue();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await stream.MoveNextAsync());
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        run.Result(trace).SampleCount.ShouldBe(1);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        run.Result(trace).SampleCount.ShouldBe(3);
    }

    private sealed record FailingCreator : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ICreationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            throw new InvalidOperationException("Resolution failed.");
    }

    [Fact]
    public async Task SharedCandidateClock_CountsEachBatchOnce()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = new Counter();
        var first = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);
        var second = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);
        var third = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [first, second, third, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(first).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Single().Time.ShouldBe(16);
        run.Result(second).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Single().Time.ShouldBe(16);
        run.Result(third).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Single().Time.ShouldBe(16);
    }

    // A counter's accumulator is a run result, so each run counts from zero with its own accumulator.
    [Fact]
    public async Task CandidateClock_CountsEachRunOnItsOwn()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = new Counter();
        var first = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);
        var second = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);

        var firstRun = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [first, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await firstRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var secondRun = algorithm.CreateRun(problem, RandomNumberGenerator.Create(43), [second, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await secondRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstRun.Result(first).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Single().Time.ShouldBe(16);
        secondRun.Result(second).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Single().Time.ShouldBe(16);
        firstRun.Result(evaluations).ShouldNotBeSameAs(secondRun.Result(evaluations));
    }

    // Reusing one analyzer for several runs gives each run an independent trace.
    [Fact]
    public async Task AnalyzerReuse_CollectsEachRunIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var trace = algorithm.Iterations.TracePopulationCandidates();
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var analyzed = receiver.Substitute(trace);
        var firstRun = analyzed.CreateRun(problem, RandomNumberGenerator.Create(42));
        await firstRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var first = firstRun.Result(trace).Snapshot();
        var secondRun = analyzed.CreateRun(problem, RandomNumberGenerator.Create(43));
        await secondRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        first.Count.ShouldBe(2);
        firstRun.Result(trace).SampleCount.ShouldBe(2);
        secondRun.Result(trace).SampleCount.ShouldBe(2);
    }

    [Fact]
    public async Task ObjectiveVectorRanking_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var quality = algorithm.Iterations.Trace(
            observation => observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective),
            [iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        run.Result(quality).By(iterations.Clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        foreach (var sample in run.Result(quality).Snapshot())
        {
            comparer.Compare(sample.Value.Best, sample.Value.Median).ShouldBeLessThanOrEqualTo(0);
            comparer.Compare(sample.Value.Median, sample.Value.Worst).ShouldBeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task CustomFunction_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var firstDimension = algorithm.Iterations.Trace(
            observation => observation.State.Population.Candidates.Select(candidate => candidate[0]).MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [firstDimension]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(firstDimension).SampleCount.ShouldBe(3);
        foreach (var sample in run.Result(firstDimension).Snapshot())
        {
            sample.Value.Min.ShouldBeLessThanOrEqualTo(sample.Value.Mean);
            sample.Value.Mean.ShouldBeLessThanOrEqualTo(sample.Value.Max);
        }
    }

    [Fact]
    public async Task CustomFunction_ComposesForACrossoverSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var offspring = algorithm.Crossover.Invocations.Trace(
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax(),
            [iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [offspring, algorithm.Iterations.Counting(iterations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the crossover fires once per later generation.
        run.Result(offspring).SampleCount.ShouldBe(2);

        // Iterations are observed at another boundary, so a sample carries the latest iteration seen before it.
        run.Result(offspring).By(iterations.Clock).Select(point => point.Time).ShouldBe([1L, 2L]);
    }

    [Fact]
    public async Task ValueTrace_ReceivesTheTypedCrossoverObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var offspringCount = algorithm.Crossover.Invocations.Trace(
            observation => observation.Offspring.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [offspringCount]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(offspringCount).Snapshot().ShouldHaveSingleItem().Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task OffspringTraces_RecordCrossoverAndMutationOffspring()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { MutationRate = 1 };
        var crossed = algorithm.Crossover.Invocations.Trace(static observation => observation.Offspring.ToImmutableArray());
        var mutated = algorithm.Mutator.Invocations.Trace(static observation => observation.Offspring.ToImmutableArray());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [crossed, mutated]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(crossed).SampleCount.ShouldBeGreaterThan(0);
        run.Result(crossed).Snapshot().Values.ShouldAllBe(offspring => offspring.Length > 0);
        run.Result(mutated).SampleCount.ShouldBeGreaterThan(0);
        run.Result(mutated).Snapshot().Values.ShouldAllBe(offspring => offspring.Length > 0);
    }

    [Fact]
    public void TracePopulationCandidates_IsAShortcutForTheSameComposition()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);

        var shortcut = algorithm.Iterations.TracePopulationCandidates();
        var composed = algorithm.Iterations.Trace(
            static observation => observation.State.Population.EvaluatedCandidates.BestMedianWorst(observation.Problem.Objective));

        shortcut.ShouldBeAssignableTo<TraceAnalyzer<AlgorithmObservation<RealVector, PopulationState<RealVector>>, BestMedianWorstEntry<RealVector>>>();
        composed.ShouldBeAssignableTo<TraceAnalyzer<AlgorithmObservation<RealVector, PopulationState<RealVector>>, BestMedianWorstEntry<RealVector>>>();
        shortcut.ShouldNotBeSameAs(composed);
    }

    [Fact]
    public async Task SeparateCompositions_CollectIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [first, second]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.ShouldNotBeSameAs(second);
        run.Result(first).SampleCount.ShouldBe(3);
        run.Result(second).SampleCount.ShouldBe(3);
        run.Result(first).ToList().ShouldNotBeSameAs(run.Result(second).ToList());
    }

    [Fact]
    public async Task ValueTrace_ComposesForAnEvaluatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var evaluations = new Counter();
        var batchSize = algorithm.Evaluator.Invocations.Trace(observation => observation.Candidates.Count, [evaluations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [batchSize, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(batchSize).SampleCount.ShouldBeGreaterThan(0);
        run.Result(batchSize).Snapshot().Values.ShouldAllBe(count => count > 0);

        // The clock shares the evaluator boundary, so a sample already counts the candidates it reports.
        run.Result(batchSize).By(evaluations.Clock).Select(point => point.Time).ShouldBeInOrder();
    }

    [Fact]
    public async Task CustomFunction_ComposesForAMutatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var mutated = algorithm.Mutator.Invocations.Trace(
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [mutated]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(mutated).SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ValueTrace_ComposesForAnInterceptorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var kept = algorithm.Interceptor.Invocations.Trace(
            observation => observation.State.Population.EvaluatedCandidates.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [kept]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(kept).Snapshot().Values.ShouldBe([5, 5, 5]);
    }

    [Fact]
    public async Task InterceptorObservation_CarriesTheStateBeforeAndAfterTheTransformation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var removed = algorithm.Interceptor.Invocations.Trace(
            observation => observation.UntransformedState.Population.EvaluatedCandidates.Count -
                           observation.State.Population.EvaluatedCandidates.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [removed]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(removed).SampleCount.ShouldBeGreaterThan(0);
        run.Result(removed).Snapshot().Values.ShouldAllBe(count => count == 11);
    }

    [Fact]
    public async Task SeveralTracesAtOneSource_AllObserveTheSameOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.Evaluator.Invocations.Trace(observation => observation.Candidates.Count);
        var second = algorithm.Evaluator.Invocations.Trace(observation => observation.ObjectiveVectors.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [first, second]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(first).SampleCount.ShouldBe(run.Result(second).SampleCount);
        run.Result(first).SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Retention_KeepsEveryObservationByDefault()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var every = algorithm.Iterations.Trace(observation => observation.Iteration);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [every]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(every).SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task EveryNthRetention_CountsFiringsRatherThanEntries()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var everyThird = algorithm.Iterations.Trace(observation => observation.Iteration,
            retention: TraceRetention.EveryNth(3));

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [everyThird]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(everyThird).Snapshot().Values.ShouldBe([3L, 6L]);
    }

    [Fact]
    public async Task OnChangeRetention_SkipsRepeatsOfTheLastRecordedValue()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 5);
        var populationSize = algorithm.Iterations.Trace(
            observation => observation.State.Population.EvaluatedCandidates.Count,
            retention: TraceRetention.OnChange());

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [populationSize]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The population size never changes, so only the first firing is kept.
        run.Result(populationSize).SampleCount.ShouldBe(1);
    }

    [Fact]
    public async Task LatestOnlyRetention_KeepsOneEntryCarryingTheMomentOfItsOwnObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var iterations = new Counter();
        var current = algorithm.Iterations.Trace(observation => observation.Iteration,
            clocks: [iterations.Clock],
            retention: TraceRetention.LatestOnly());

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [current, algorithm.Iterations.Counting(iterations)]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Every firing replaced the one before it, so the trace holds the last one and its own moment.
        run.Result(current).SampleCount.ShouldBe(1);
        run.Result(current).RequireLatestValue().ShouldBe(6L);
        run.Result(current).By(iterations.Clock).Select(point => point.Time).ShouldBe([6L]);
    }

    [Fact]
    public async Task BestSoFar_RecordsOnlyImprovements()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var evaluations = new Counter();
        var best = Analyzer.BestSoFar(algorithm.Evaluator, [evaluations.Clock]);
        var batches = algorithm.Evaluator.Invocations.Trace(observation => observation.ObjectiveVectors.Min(vector => vector[0]), [evaluations.Clock]);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [best, batches, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        var curve = run.Result(best).ToList();
        curve.Count.ShouldBeGreaterThan(0);
        var expected = new List<(long Evaluations, double Value)>();
        var runningBest = double.PositiveInfinity;
        foreach (var batch in run.Result(batches))
            if (batch.Value < runningBest)
            {
                runningBest = batch.Value;
                expected.Add((batch.At(evaluations.Clock), batch.Value));
            }
        curve.Select(point => (point.Evaluations, point.Value[0])).ShouldBe(expected);

        // Every recorded entry is strictly better than the one before it, against a rising evaluation count.
        foreach (var (previous, current) in curve.Zip(curve.Skip(1)))
            comparer.Compare(current.Value, previous.Value).ShouldBeLessThan(0);

        run.Result(best).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).Select(point => point.Time).ShouldBeInOrder();
    }

    /// <summary>A custom analyzer can place its own wrapper at an execution boundary the library does not observe.</summary>
    [Fact]
    public async Task CustomAnalyzer_ObservesABoundaryTheLibraryDoesNotCover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var created = new CreatorCountAnalyzer(algorithm.Creator);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [created]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The creator fills the initial population once, so one firing of 16 candidates.
        created.CreatedCandidates.ShouldBe(16);
    }

    // The caller places a clock that observes a boundary; a trace only reads its run's count.
    [Fact]
    public async Task CustomObservingClock_IsPlacedByTheCaller_AndTraceAloneLeavesItAtZero()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var crossoverCalls = new CrossoverCallClock(algorithm.Crossover);
        var quality = algorithm.Iterations.TracePopulationCandidates([crossoverCalls]);

        var unplaced = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality]);
        await unplaced.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        unplaced.Result(quality).By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 0L, 0L]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, crossoverCalls]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the first generation is recorded at zero calls.
        run.Result(quality).By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    private sealed class CrossoverCallClock(IOperator<Crossover, RealVector> crossover)
        : SubstitutionGroup, IClock<long>, IObservationReceiver<CrossoverObservation<RealVector>>
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [crossover.Invocations.RecordingInto(this)];

        private readonly Counter counter = new();

        public Action<CrossoverObservation<RealVector>> CreateRecorder(RunResults results)
        {
            var count = results.Result(counter);
            return _ => count.IncrementBy(1);
        }

        public IClockReading<long> CreateExecutionNode(ResolutionScope scope) => new CallCount(scope.Results.Result(counter));
    }

    private sealed class CreatorCountAnalyzer(IOperator<Creation, RealVector> creator) : SubstitutionGroup
    {
        public int CreatedCandidates { get; private set; }

        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
            [Substitution.Of(creator, child => new ObservedCreator(child, count => CreatedCandidates += count))];
    }

    // Reports how many candidates each call of the creator it wraps returned.
    private sealed record ObservedCreator : WrappingOperator<Creation, RealVector>
    {
        public ObservedCreator(IOperator<Creation, RealVector> childCreator, Action<int> observe)
            : base(childCreator)
        {
            Observe = observe;
        }

        public Action<int> Observe { get; init; }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            var childOperator = scope.Resolve(ChildOperator);
            return new Executor(childOperator, Observe);
        }

        private sealed class Executor(
            ICreationExecutor<RealVector> childCreator,
            Action<int> observe) : CreationExecutor<RealVector>
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random)
            {
                var candidates = childCreator.Create(count, random);
                observe(candidates.Count);
                return candidates;
            }
        }
    }

    /// <summary>
    /// The order a ranking function uses is the run's, not the caller's. The same composition therefore ranks
    /// the other way round when the run it is given to optimizes the other way, with nothing said at composition time.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ranking_TakesItsOrderingFromTheRun(bool maximize)
    {
        var objective = maximize ? SingleObjective.Maximize : SingleObjective.Minimize;
        var searchSpace = new BoundedRealVectorSearchSpace(length: 2, minimum: -5.12, maximum: 5.12);
        var problem = new FuncProblem<RealVector, BoundedRealVectorSearchSpace>(
            static (RealVector candidate) => candidate[0], searchSpace, objective);
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 16,
            MaximumGenerations = 2,
            Creator = new UniformDistributedCreator(searchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2
        };

        // Nothing here names an objective.
        var quality = algorithm.Iterations.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var sample = run.Result(quality).Snapshot()[0].Value;
        if (maximize)
            sample.Best.ObjectiveVector[0].ShouldBeGreaterThan(sample.Worst.ObjectiveVector[0]);
        else
            sample.Best.ObjectiveVector[0].ShouldBeLessThan(sample.Worst.ObjectiveVector[0]);
    }

    private sealed record TruncatingInterceptor(int KeptCandidates)
        : StatelessInterceptor<RealVector, PopulationState<RealVector>>
    {
        public override PopulationState<RealVector> Transform(PopulationState<RealVector> currentState,
                                                              PopulationState<RealVector>? previousState,
                                                              IRandomNumberGenerator random) =>
            currentState with { Population = Population.From(currentState.Population.EvaluatedCandidates.Take(KeptCandidates)) };
    }

    private static TestFunctionProblem CreateProblem() => new(new RastriginFunction(dimension: 4));

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(
        TestFunctionProblem problem, int maximumGenerations) =>
        new()
        {
            PopulationSize = 16,
            MaximumGenerations = maximumGenerations,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1
        };
}
