using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers composing a trace from an observation source, a measurement and an aggregation.
/// </summary>
public class TraceCompositionTests
{
    [Fact]
    public async Task DuplicateAnalyzerAttachment_RecordsEachObservationOnce()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.TracePopulationQuality();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace).Attach(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        trace.SampleCount.ShouldBe(1);
    }

    [Fact]
    public void InstallationFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace).Attach(new FailingModule());
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        trace.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void ResolutionFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1) with { Creator = new FailingCreator() };
        var trace = algorithm.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace);
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        trace.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task EarlyStreamDisposal_PreservesTraceForAnotherRun()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace);
        await using (var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                     .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await stream.MoveNextAsync()).ShouldBeTrue();
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        trace.SampleCount.ShouldBe(1);
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).Attach(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        trace.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task Cancellation_PausesTheRunAndPreservesCollectedSamples()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.TracePopulationQuality();
        using var cancellation = new CancellationTokenSource();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace);
        await using var stream = run.Stream(cancellationToken: cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        (await stream.MoveNextAsync()).ShouldBeTrue();
        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await stream.MoveNextAsync());
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        trace.SampleCount.ShouldBe(1);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.LifecycleState.ShouldBe(RunLifecycleState.Completed);
        trace.SampleCount.ShouldBe(3);
    }

    private sealed class FailingModule : IExecutionModule
    {
        public void Install(ResolutionScopeBuilder builder) => throw new InvalidOperationException("Installation failed.");
    }

    private sealed record FailingCreator : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override ICreatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ResolutionScope scope) =>
            throw new InvalidOperationException("Resolution failed.");
    }

    [Fact]
    public async Task SharedEvaluationClock_CountsEachBatchOnce()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var first = algorithm.Evaluator.TraceBestCandidateSoFar([evaluations]);
        var second = algorithm.Evaluator.TraceBestCandidateSoFar([evaluations]);
        var third = algorithm.Evaluator.TraceBestCandidateSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(first).Attach(second).Attach(third)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.By(evaluations).Single().Time.ShouldBe(16);
        second.By(evaluations).Single().Time.ShouldBe(16);
        third.By(evaluations).Single().Time.ShouldBe(16);
    }

    [Fact]
    public async Task EvaluationClock_CanAccumulateAcrossRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var first = algorithm.Evaluator.TraceBestSoFar([evaluations]);
        var second = algorithm.Evaluator.TraceBestSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(first)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).Attach(second)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.By(evaluations).Single().Time.ShouldBe(16);
        second.By(evaluations).Single().Time.ShouldBe(32);
    }

    [Fact]
    public async Task AnalyzerReuse_AccumulatesAcrossRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var trace = algorithm.TracePopulationCandidates();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).Attach(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var first = trace.Snapshot();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).Attach(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        first.Count.ShouldBe(2);
        trace.SampleCount.ShouldBe(4);
    }

    [Fact]
    public void CommonMeasurements_AreSerializableValueObjects()
    {
        var measurement = new ObjectiveVectorsMeasurement<RealVector, PopulationState<RealVector>>();

        var json = System.Text.Json.JsonSerializer.Serialize(measurement);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ObjectiveVectorsMeasurement<RealVector, PopulationState<RealVector>>>(json);

        roundTrip.ShouldBe(measurement);
    }

    [Fact]
    public async Task ObjectiveMeasurement_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(algorithm,
            new ObjectiveVectorsMeasurement<RealVector, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst(),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        quality.By(iterations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        foreach (var sample in quality.Snapshot())
        {
            comparer.Compare(sample.Value.Best, sample.Value.Median).ShouldBeLessThanOrEqualTo(0);
            comparer.Compare(sample.Value.Median, sample.Value.Worst).ShouldBeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var firstDimension = Analyzer.Trace(algorithm,
            observation => [.. observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.Candidate[0])],
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(firstDimension);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstDimension.SampleCount.ShouldBe(3);
        foreach (var sample in firstDimension.Snapshot())
        {
            sample.Value.Min.ShouldBeLessThanOrEqualTo(sample.Value.Mean);
            sample.Value.Mean.ShouldBeLessThanOrEqualTo(sample.Value.Max);
        }
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesForACrossoverSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var offspring = Analyzer.Trace(algorithm.Crossover,
            observation => [.. observation.Offspring.Select(candidate => candidate[0])],
            Aggregate.MinMeanMax(),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(offspring);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the crossover fires once per later generation.
        offspring.SampleCount.ShouldBe(2);

        // Iterations are observed at another boundary, so a sample carries the latest iteration seen before it.
        offspring.By(iterations).Select(point => point.Time).ShouldBe([1L, 2L]);
    }

    [Fact]
    public async Task CustomMeasurement_ReceivesTheTypedCrossoverObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var offspringCount = Analyzer.Trace(algorithm.Crossover,
            new OffspringCountMeasurement(),
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(offspringCount);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        offspringCount.Snapshot().ShouldHaveSingleItem().Value.Mean.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CrossoverMeasurementsUseTheTypedObservation()
    {
        typeof(IMeasurement<CrossoverObservation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>, double>)
            .IsAssignableFrom(typeof(OffspringCountMeasurement)).ShouldBeTrue();

        // This is intentionally absent because it must not compile:
        // Analyzer.Trace(algorithm.Crossover, new ObjectiveVectorsMeasurement<...>(), Aggregate.BestMedianWorst());
    }

    private sealed record OffspringCountMeasurement
        : IMeasurement<CrossoverObservation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>, double>
    {
        public IReadOnlyList<double> Read(
            CrossoverObservation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> observation) =>
            [observation.Offspring.Count];
    }

    [Fact]
    public void TracePopulationCandidates_IsAShortcutForTheSameComposition()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);

        var shortcut = algorithm.TracePopulationCandidates();
        var composed = Analyzer.Trace(algorithm,
            new EvaluatedCandidatesMeasurement<RealVector, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst<RealVector>());

        shortcut.ShouldBeAssignableTo<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        composed.ShouldBeAssignableTo<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        shortcut.ShouldNotBeSameAs(composed);
    }

    [Fact]
    public async Task SeparateCompositions_CollectIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.TracePopulationCandidates();
        var second = algorithm.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(first).Attach(second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
        first.Snapshot().ShouldNotBeSameAs(second.Snapshot());
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesForAnEvaluatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var batchSize = Analyzer.Trace(algorithm.Evaluator,
            observation => (IReadOnlyList<double>)[observation.Candidates.Count],
            Aggregate.MinMeanMax(),
            [evaluations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(batchSize);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        batchSize.SampleCount.ShouldBeGreaterThan(0);
        foreach (var sample in batchSize.Snapshot())
            sample.Value.Min.ShouldBeGreaterThan(0);

        // The clock shares the evaluator boundary, so a sample already counts the candidates it reports.
        batchSize.By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesForAMutatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var mutated = Analyzer.Trace(algorithm.Mutator,
            observation => [.. observation.Offspring.Select(candidate => candidate[0])],
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(mutated);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        mutated.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesForAnInterceptorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var kept = Analyzer.Trace<RealVector, PopulationState<RealVector>, double, MinMeanMax>(algorithm.Interceptor!,
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(kept);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        kept.SampleCount.ShouldBe(3);
        foreach (var sample in kept.Snapshot())
            sample.Value.Max.ShouldBe(5);
    }

    [Fact]
    public async Task InterceptorObservation_CarriesTheStateBeforeAndAfterTheTransformation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var removed = Analyzer.Trace<RealVector, PopulationState<RealVector>, double, MinMeanMax>(algorithm.Interceptor!,
            observation => (IReadOnlyList<double>)
            [
                observation.UntransformedState.Population.EvaluatedCandidates.Count -
                observation.State.Population.EvaluatedCandidates.Count
            ],
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(removed);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var sample in removed.Snapshot())
            sample.Value.Min.ShouldBe(11);
    }

    [Fact]
    public async Task SeveralTracesAtOneSource_AllObserveTheSameOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.Trace(algorithm.Evaluator,
            observation => (IReadOnlyList<double>)[observation.Candidates.Count],
            Aggregate.MinMeanMax());
        var second = Analyzer.Trace(algorithm.Evaluator,
            observation => (IReadOnlyList<double>)[observation.ObjectiveVectors.Count],
            Aggregate.MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(first).Attach(second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(second.SampleCount);
        first.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Retention_KeepsEveryObservationByDefault()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var every = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.Iteration],
            Aggregate.MinMeanMax());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(every)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        every.SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task EveryNthRetention_CountsFiringsRatherThanEntries()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var everyThird = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.Iteration],
            Aggregate.MinMeanMax(),
            retention: TraceRetention.EveryNth(3));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(everyThird)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        everyThird.Snapshot().Select(entry => entry.Value.Max).ShouldBe([3d, 6d]);
    }

    [Fact]
    public async Task OnChangeRetention_SkipsRepeatsOfTheLastRecordedValue()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 5);
        var populationSize = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax(),
            retention: TraceRetention.OnChange());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(populationSize)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The population size never changes, so only the first firing is kept.
        populationSize.SampleCount.ShouldBe(1);
    }

    [Fact]
    public async Task LatestOnlyRetention_KeepsOneEntryCarryingTheMomentOfItsOwnObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var iterations = Clock.FromIterations(algorithm);
        var current = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.Iteration],
            Aggregate.MinMeanMax(),
            clocks: [iterations],
            retention: TraceRetention.LatestOnly());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(current)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Every firing replaced the one before it, so the trace holds the last one and its own moment.
        current.SampleCount.ShouldBe(1);
        current.RequireLatestValue().Max.ShouldBe(6d);
        current.By(iterations).Select(point => point.Time).ShouldBe([6L]);
    }

    [Fact]
    public async Task TraceBestCandidateSoFar_RecordsOnlyImprovements()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var best = algorithm.Evaluator.TraceBestCandidateSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(best)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        var curve = best.Snapshot();
        curve.Count.ShouldBeGreaterThan(0);

        // Every recorded entry is strictly better than the one before it, against a rising evaluation count.
        foreach (var (previous, current) in curve.Zip(curve.Skip(1)))
            comparer.Compare(current.Value.ObjectiveVector, previous.Value.ObjectiveVector).ShouldBeLessThan(0);

        best.By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    /// <summary>A custom analyzer can install a module for an execution boundary the library does not observe.</summary>
    [Fact]
    public async Task CustomAnalyzer_ObservesABoundaryTheLibraryDoesNotCover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var created = new CreatorCountAnalyzer(algorithm.Creator);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(created)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The creator fills the initial population once, so one firing of 16 candidates.
        created.CreatedCandidates.ShouldBe(16);
    }

    /// <summary>
    /// A clock over a boundary is written by deriving from <see cref="Clock{TTime}"/> and observing its source, which
    /// leaves the clock with reading an observation and reporting the time.
    /// </summary>
    [Fact]
    public async Task CustomObservingClock_InstallsItselfForItsSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var crossoverCalls = new CrossoverCallClock(algorithm.Crossover);
        var quality = algorithm.TracePopulationCandidates([crossoverCalls]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the first generation is recorded at zero calls.
        quality.By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    private sealed class CrossoverCallClock(ICrossover<RealVector> crossover)
        : Clock<long>
    {
        private long calls;

        protected override long ReadTime() => Interlocked.Read(ref calls);

        public override void Install(ResolutionScopeBuilder builder) =>
            builder.Observe(crossover, _ => Interlocked.Increment(ref calls));
    }

    private sealed class CreatorCountAnalyzer(ICreator<RealVector> creator)
        : IAnalyzer
    {
        public int CreatedCandidates { get; private set; }

        public void Install(ResolutionScopeBuilder builder) =>
            builder.Install(new CreatorObservationModule(creator, count => CreatedCandidates += count));
    }

    private sealed class CreatorObservationModule(
        ICreator<RealVector> creator,
        Action<int> observe) : IExecutionModule
    {
        public void Install(ResolutionScopeBuilder builder) =>
            builder.Wrap(creator, current => new ObservingCreator(current, observe));
    }

    private sealed record ObservingCreator(
        ICreator<RealVector> Child,
        Action<int> Observe) : WrappingCreator<RealVector>(Child)
    {
        protected override ICreatorExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorExecution<RealVector, TRunSearchSpace, TRunProblem> childCreator) =>
            new Execution<TRunSearchSpace, TRunProblem>(childCreator, Observe);

        private sealed class Execution<TSearchSpace, TProblem>(
            ICreatorExecution<RealVector, TSearchSpace, TProblem> childCreator,
            Action<int> observe) : ICreatorExecution<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var candidates = childCreator.Create(count, random, searchSpace, problem);
                observe(candidates.Count);
                return candidates;
            }
        }
    }

    /// <summary>
    /// The comparer a ranking aggregation uses is the run's, not the caller's. The same composition therefore ranks
    /// the other way round when the run it is given to optimizes the other way, with nothing said at composition time.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RankingAggregation_TakesItsOrderingFromTheRun(bool maximize)
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
        var quality = algorithm.TracePopulationCandidates();

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var sample = quality.Snapshot()[0].Value;
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
            currentState with { Population = Population.From(currentState.Population.Take(KeptCandidates)) };
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
