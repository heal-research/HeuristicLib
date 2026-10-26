using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers composing a trace from an observation source and a function of each observation.
/// </summary>
public class TraceCompositionTests
{
    [Fact]
    public async Task DuplicateAnalyzerAttachment_RecordsEachObservationOnce()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.Iterations.TracePopulationQuality();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace).AddAspect(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        trace.SampleCount.ShouldBe(1);
    }

    [Fact]
    public void InstallationFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace).AddAspect(new FailingModule());
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        trace.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void ResolutionFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1) with { Creator = new FailingCreator() };
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace);
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        trace.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task EarlyStreamDisposal_PreservesTraceForAnotherRun()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace);
        await using (var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                     .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await stream.MoveNextAsync()).ShouldBeTrue();
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        trace.SampleCount.ShouldBe(1);
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).AddAspect(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        trace.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task Cancellation_PausesTheRunAndPreservesCollectedSamples()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var trace = algorithm.Iterations.TracePopulationQuality();
        using var cancellation = new CancellationTokenSource();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace);
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

    private sealed class FailingModule : Aspect
    {
        public FailingModule()
        {
            Advices = [(Pointcut.OfType<IOperator<Creation, RealVector>>(), new FailingAdvice())];
        }
    }

    private sealed class FailingAdvice : CreatorAdvice<RealVector>
    {
        protected override void Check<TRunSearchSpace, TRunProblem>(IOperator<Creation, RealVector> creator) =>
            throw new InvalidOperationException("Advice preparation failed.");

        protected override ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> Wrap<TRunSearchSpace, TRunProblem>(
            ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> executor, IOperator<Creation, RealVector> creator) => executor;

    }

    private sealed record FailingCreator : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor(ResolutionScope scope) =>
            throw new InvalidOperationException("Resolution failed.");
    }

    [Fact]
    public async Task SharedCandidateClock_CountsEachBatchOnce()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = Clock.FromCandidates(algorithm.Evaluator);
        var first = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);
        var second = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);
        var third = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(first).AddAspect(second).AddAspect(third)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.By(evaluations).Single().Time.ShouldBe(16);
        second.By(evaluations).Single().Time.ShouldBe(16);
        third.By(evaluations).Single().Time.ShouldBe(16);
    }

    [Fact]
    public async Task CandidateClock_CanAccumulateAcrossRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = Clock.FromCandidates(algorithm.Evaluator);
        var first = algorithm.Evaluator.Invocations.TraceBestSoFar([evaluations]);
        var second = algorithm.Evaluator.Invocations.TraceBestSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(first)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).AddAspect(second)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.By(evaluations).Single().Time.ShouldBe(16);
        second.By(evaluations).Single().Time.ShouldBe(32);
    }

    [Fact]
    public async Task AnalyzerReuse_AccumulatesAcrossRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var trace = algorithm.Iterations.TracePopulationCandidates();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAspect(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var first = trace.Snapshot();
        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).AddAspect(trace)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        first.Count.ShouldBe(2);
        trace.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task ObjectiveVectorRanking_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(algorithm.Iterations,
            observation => observation.State.Population.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality);

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
    public async Task CustomFunction_ComposesForAnAlgorithmSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var firstDimension = Analyzer.Trace(algorithm.Iterations,
            observation => observation.State.Population.Candidates.Select(candidate => candidate[0]).MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(firstDimension);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstDimension.SampleCount.ShouldBe(3);
        foreach (var sample in firstDimension.Snapshot())
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
        var iterations = Clock.FromIterations(algorithm);
        var offspring = Analyzer.Trace(algorithm.Crossover.Invocations,
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax(),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(offspring);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the crossover fires once per later generation.
        offspring.SampleCount.ShouldBe(2);

        // Iterations are observed at another boundary, so a sample carries the latest iteration seen before it.
        offspring.By(iterations).Select(point => point.Time).ShouldBe([1L, 2L]);
    }

    [Fact]
    public async Task ValueTrace_ReceivesTheTypedCrossoverObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var offspringCount = Analyzer.Trace(algorithm.Crossover.Invocations,
            observation => observation.Offspring.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(offspringCount);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        offspringCount.Snapshot().ShouldHaveSingleItem().Value.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task OffspringTraces_RecordCrossoverAndMutationOffspring()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { MutationRate = 1 };
        var crossed = Analyzer.Trace(algorithm.Crossover.Invocations, static observation => observation.Offspring.ToImmutableArray());
        var mutated = Analyzer.Trace(algorithm.Mutator.Invocations, static observation => observation.Offspring.ToImmutableArray());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(crossed).AddAspect(mutated);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        crossed.SampleCount.ShouldBeGreaterThan(0);
        crossed.Snapshot().Values.ShouldAllBe(offspring => offspring.Length > 0);
        mutated.SampleCount.ShouldBeGreaterThan(0);
        mutated.Snapshot().Values.ShouldAllBe(offspring => offspring.Length > 0);
    }

    [Fact]
    public void TracePopulationCandidates_IsAShortcutForTheSameComposition()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);

        var shortcut = algorithm.Iterations.TracePopulationCandidates();
        var composed = Analyzer.Trace(algorithm.Iterations,
            static observation => observation.State.Population.BestMedianWorst(observation.Problem.Objective));

        shortcut.ShouldBeAssignableTo<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        composed.ShouldBeAssignableTo<TraceAnalyzer<BestMedianWorstEntry<RealVector>>>();
        shortcut.ShouldNotBeSameAs(composed);
    }

    [Fact]
    public async Task SeparateCompositions_CollectIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(first).AddAspect(second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
        first.Snapshot().ShouldNotBeSameAs(second.Snapshot());
    }

    [Fact]
    public async Task ValueTrace_ComposesForAnEvaluatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var evaluations = Clock.FromCandidates(algorithm.Evaluator);
        var batchSize = Analyzer.Trace(algorithm.Evaluator.Invocations, observation => observation.Candidates.Count, [evaluations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(batchSize);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        batchSize.SampleCount.ShouldBeGreaterThan(0);
        batchSize.Snapshot().Values.ShouldAllBe(count => count > 0);

        // The clock shares the evaluator boundary, so a sample already counts the candidates it reports.
        batchSize.By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    [Fact]
    public async Task CustomFunction_ComposesForAMutatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var mutated = Analyzer.Trace(algorithm.Mutator.Invocations,
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax());
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(mutated);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        mutated.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ValueTrace_ComposesForAnInterceptorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var kept = Analyzer.Trace(algorithm.Interceptor.Invocations,
            observation => observation.State.Population.EvaluatedCandidates.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(kept);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        kept.Snapshot().Values.ShouldBe([5, 5, 5]);
    }

    [Fact]
    public async Task InterceptorObservation_CarriesTheStateBeforeAndAfterTheTransformation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var removed = Analyzer.Trace(algorithm.Interceptor.Invocations,
            observation => observation.UntransformedState.Population.EvaluatedCandidates.Count -
                           observation.State.Population.EvaluatedCandidates.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(removed);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        removed.SampleCount.ShouldBeGreaterThan(0);
        removed.Snapshot().Values.ShouldAllBe(count => count == 11);
    }

    [Fact]
    public async Task SeveralTracesAtOneSource_AllObserveTheSameOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.Trace(algorithm.Evaluator.Invocations, observation => observation.Candidates.Count);
        var second = Analyzer.Trace(algorithm.Evaluator.Invocations, observation => observation.ObjectiveVectors.Count);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(first).AddAspect(second);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(second.SampleCount);
        first.SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Retention_KeepsEveryObservationByDefault()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var every = Analyzer.Trace(algorithm.Iterations, observation => observation.Iteration);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(every)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        every.SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task EveryNthRetention_CountsFiringsRatherThanEntries()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var everyThird = Analyzer.Trace(algorithm.Iterations, observation => observation.Iteration,
            retention: TraceRetention.EveryNth(3));

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(everyThird)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        everyThird.Snapshot().Values.ShouldBe([3L, 6L]);
    }

    [Fact]
    public async Task OnChangeRetention_SkipsRepeatsOfTheLastRecordedValue()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 5);
        var populationSize = Analyzer.Trace(algorithm.Iterations,
            observation => observation.State.Population.EvaluatedCandidates.Count,
            retention: TraceRetention.OnChange());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(populationSize)
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
        var current = Analyzer.Trace(algorithm.Iterations, observation => observation.Iteration,
            clocks: [iterations],
            retention: TraceRetention.LatestOnly());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(current)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Every firing replaced the one before it, so the trace holds the last one and its own moment.
        current.SampleCount.ShouldBe(1);
        current.RequireLatestValue().ShouldBe(6L);
        current.By(iterations).Select(point => point.Time).ShouldBe([6L]);
    }

    [Fact]
    public async Task TraceBestCandidateSoFar_RecordsOnlyImprovements()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var evaluations = Clock.FromCandidates(algorithm.Evaluator);
        var best = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(best)
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

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(created)
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
        var quality = algorithm.Iterations.TracePopulationCandidates([crossoverCalls]);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the first generation is recorded at zero calls.
        quality.By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    private sealed class CrossoverCallClock
        : Clock<long>
    {
        private long calls;

        protected override long ReadTime() => Interlocked.Read(ref calls);

        public CrossoverCallClock(IOperator<Crossover, RealVector> crossover)
        {
            Advices = [.. Aspect.Observe(crossover.Invocations, _ => Interlocked.Increment(ref calls))];
        }
    }

    private sealed class CreatorCountAnalyzer
        : Aspect
    {
        public int CreatedCandidates { get; private set; }

        public CreatorCountAnalyzer(IOperator<Creation, RealVector> creator)
        {
            Advices = [(Pointcut.Reference(creator), new ObservingCreatorAdvice(count => CreatedCandidates += count))];
        }
    }

    private sealed class ObservingCreatorAdvice(Action<int> observe) : CreatorAdvice<RealVector>
    {
        protected override ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> Wrap<TRunSearchSpace, TRunProblem>(
            ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> executor, IOperator<Creation, RealVector> creator) =>
            new Executor<TRunSearchSpace, TRunProblem>(executor, observe);

        private sealed class Executor<TSearchSpace, TProblem>(
            ICreationExecutor<RealVector, TSearchSpace, TProblem> childCreator,
            Action<int> observe) : ICreationExecutor<RealVector, TSearchSpace, TProblem>
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

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality)
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
