using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers composing a trace from a measurement, an aggregation and an anchor.
/// </summary>
public class TraceCompositionTests
{
    [Fact]
    public void CommonMeasurements_AreSerializableValueObjects()
    {
        var measurement = new ObjectiveVectorsMeasurement<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>();

        var json = System.Text.Json.JsonSerializer.Serialize(measurement);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ObjectiveVectorsMeasurement<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>>(json);

        roundTrip.ShouldBe(measurement);
    }

    [Fact]
    public async Task ObjectiveMeasurement_ComposesAtAnAlgorithmAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(
            new ObjectiveVectorsMeasurement<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst(),
            Anchor.At(algorithm),
            iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);

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
    public async Task CandidateMeasurement_ComposesAtAnAlgorithmAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var firstDimension = Analyzer.Trace(
            observation => [.. observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.Candidate[0])],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), firstDimension);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstDimension.SampleCount.ShouldBe(3);
        foreach (var sample in firstDimension.Snapshot())
        {
            sample.Value.Min.ShouldBeLessThanOrEqualTo(sample.Value.Mean);
            sample.Value.Mean.ShouldBeLessThanOrEqualTo(sample.Value.Max);
        }
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesAtACrossoverAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var offspring = Analyzer.Trace(
            observation => [.. observation.Offspring.Select(candidate => candidate[0])],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Crossover),
            iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), offspring);

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
        var offspringCount = Analyzer.Trace(
            new OffspringCountMeasurement(),
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Crossover));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), offspringCount);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        offspringCount.Snapshot().ShouldHaveSingleItem().Value.Mean.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CrossoverMeasurementsUseTheTypedObservation()
    {
        typeof(IMeasurement<CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem>, double>)
            .IsAssignableFrom(typeof(OffspringCountMeasurement)).ShouldBeTrue();

        // This is intentionally absent because it must not compile:
        // Analyzer.Trace(new ObjectiveVectorsMeasurement<...>(), Aggregate.BestMedianWorst(), Anchor.At(algorithm.Crossover));
    }

    private sealed record OffspringCountMeasurement
        : IMeasurement<CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem>, double>
    {
        public IReadOnlyList<double> Read(
            CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem> observation) =>
            [observation.Offspring.Count];
    }

    [Fact]
    public void TraceBestMedianWorst_IsAShortcutForTheSameComposition()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);

        var shortcut = Analyzer.TraceBestMedianWorst(algorithm);
        var composed = Analyzer.Trace(
            new EvaluatedCandidatesMeasurement<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst<RealVector>(),
            Anchor.At(algorithm));

        shortcut.ShouldBeOfType<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        composed.ShouldBeOfType<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        shortcut.ShouldNotBeSameAs(composed);
    }

    [Fact]
    public void MeasurementsAndAggregations_CompareStructurally()
    {
        Aggregate.BestMedianWorst().ShouldBe(Aggregate.BestMedianWorst());
        Aggregate.MinMeanMax().ShouldBe(Aggregate.MinMeanMax());
    }

    [Fact]
    public async Task SeparateCompositions_CollectIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.TraceBestMedianWorst(algorithm);
        var second = Analyzer.TraceBestMedianWorst(algorithm);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), first, second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
        first.Snapshot().ShouldNotBeSameAs(second.Snapshot());
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesAtAnEvaluatorAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var batchSize = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.Candidates.Count],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Evaluator),
            evaluations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), batchSize);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        batchSize.SampleCount.ShouldBeGreaterThan(0);
        foreach (var sample in batchSize.Snapshot())
            sample.Value.Min.ShouldBeGreaterThan(0);

        // The clock shares the evaluator boundary, so a sample already counts the candidates it reports.
        batchSize.By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesAtAMutatorAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var mutated = Analyzer.Trace(
            observation => [.. observation.Offspring.Select(candidate => candidate[0])],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Mutator));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), mutated);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        mutated.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CandidateMeasurement_ComposesAtAnInterceptorAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var kept = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Interceptor!));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), kept);

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
        var removed = Analyzer.Trace(
            observation => (IReadOnlyList<double>)
            [
                observation.UntransformedState.Population.EvaluatedCandidates.Count -
                observation.State.Population.EvaluatedCandidates.Count
            ],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Interceptor!));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), removed);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var sample in removed.Snapshot())
            sample.Value.Min.ShouldBe(11);
    }

    [Fact]
    public async Task SeveralTracesAtOneAnchor_AllObserveTheSameOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.Candidates.Count],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Evaluator));
        var second = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.ObjectiveVectors.Count],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm.Evaluator));
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), first, second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(second.SampleCount);
        first.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Retention_RecordsEveryFiringByDefault()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var every = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.Iteration],
            Aggregate.MinMeanMax(),
            Anchor.At(algorithm));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), every)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        every.SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task EveryNthRetention_CountsFiringsRatherThanEntries()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var everyThird = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.Iteration],
            Aggregate.MinMeanMax(),
            Retain.EveryNth<MinMeanMax>(3),
            Anchor.At(algorithm));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), everyThird)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        everyThird.Snapshot().Select(entry => entry.Value.Max).ShouldBe([3d, 6d]);
    }

    [Fact]
    public async Task OnChangeRetention_SkipsRepeatsOfTheLastRecordedValue()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 5);
        var populationSize = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax(),
            Retain.OnChange<MinMeanMax>(),
            Anchor.At(algorithm));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), populationSize)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The population size never changes, so only the first firing is kept.
        populationSize.SampleCount.ShouldBe(1);
    }

    [Fact]
    public async Task TraceBestQuality_RecordsOnlyImprovements()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var best = Analyzer.TraceBestQuality(algorithm.Evaluator, evaluations);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), best)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        var curve = best.Snapshot();
        curve.Count.ShouldBeGreaterThan(0);

        // Every recorded entry is strictly better than the one before it, against a rising evaluation count.
        foreach (var (previous, current) in curve.Zip(curve.Skip(1)))
            comparer.Compare(current.Value.ObjectiveVector, previous.Value.ObjectiveVector).ShouldBeLessThan(0);

        best.By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    /// <summary>
    /// The anchor is the extension point for a boundary the library does not observe. Creators have no anchor of their
    /// own, so a custom one has to carry its own observation, wrapper and installation.
    /// </summary>
    [Fact]
    public async Task CustomAnchor_ObservesABoundaryTheLibraryDoesNotCover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var created = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.Candidates.Count],
            Aggregate.MinMeanMax(),
            new CreatorAnchor(algorithm.Creator));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), created)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The creator fills the initial population once, so one firing of 16 candidates.
        created.Snapshot().ShouldHaveSingleItem().Value.Mean.ShouldBe(16);
    }

    /// <summary>
    /// A clock over a boundary is written by deriving from <see cref="ObservingClock{TTime, TObservation}"/>, which
    /// leaves the clock with reading an observation and reporting the time.
    /// </summary>
    [Fact]
    public async Task CustomObservingClock_InstallsItselfAtItsAnchor()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var crossoverCalls = new CrossoverCallClock(Anchor.At(algorithm.Crossover));
        var quality = Analyzer.TraceBestMedianWorst(algorithm, crossoverCalls);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the first generation is recorded at zero calls.
        quality.By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    private sealed class CrossoverCallClock(IAnchor<CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem>> anchor)
        : ObservingClock<long, CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem>>(anchor)
    {
        private long calls;

        protected override long ReadTime() => Interlocked.Read(ref calls);

        public override void Record(CrossoverObservation<RealVector, RealVectorSearchSpace, TestFunctionProblem> observation) =>
            Interlocked.Increment(ref calls);
    }

    private sealed record CreatorObservation(IReadOnlyList<RealVector> Candidates, TestFunctionProblem Problem)
        : Observation<TestFunctionProblem>(Problem);

    private sealed class CreatorAnchor(ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> creator)
        : IAnchor<CreatorObservation, TestFunctionProblem>
    {
        public void Install(ExecutionInstanceResolverBuilder builder, IObservationRecorder<CreatorObservation> recorder) =>
            builder.Decorate(creator, current => new ObservingCreator(current, recorder));
    }

    private sealed record ObservingCreator(
        ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Child,
        IObservationRecorder<CreatorObservation> Recorder)
        : WrappingCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Child)
    {
        protected override WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(
            ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childCreator) =>
            new Instance(childCreator, Recorder);

        private sealed class Instance(
            ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childCreator,
            IObservationRecorder<CreatorObservation> recorder)
            : WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childCreator)
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var candidates = ChildCreator.Create(count, random, searchSpace, problem);
                recorder.Record(new CreatorObservation(candidates, problem));
                return candidates;
            }
        }
    }

    /// <summary>
    /// The ordering a ranking aggregation uses is the run's, not the caller's. The same composition therefore ranks
    /// the other way round when the run it is given to optimizes the other way, with nothing said at composition time.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RankingAggregation_TakesItsOrderingFromTheRun(bool maximize)
    {
        var objective = maximize ? SingleObjective.Maximize : SingleObjective.Minimize;
        var searchSpace = new RealVectorSearchSpace(length: 2, minimum: -5.12, maximum: 5.12);
        var problem = new FuncProblem<RealVector, RealVectorSearchSpace>(
            static (RealVector candidate) => candidate[0], searchSpace, objective);
        var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, FuncProblem<RealVector, RealVectorSearchSpace>>
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
        var quality = Analyzer.TraceBestMedianWorst(algorithm);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality)
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

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
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
