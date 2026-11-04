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
        var run = algorithm.Advised(trace, trace).Advised(trace).CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Result(trace).SampleCount.ShouldBe(1);
    }

    [Fact]
    public void InstallationFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.Advised(new FailingModule()).Advised(trace).CreateRun(problem, RandomNumberGenerator.Create(42));
        Should.Throw<InvalidOperationException>(() => run.Stream());
        run.LifecycleState.ShouldBe(RunLifecycleState.Failed);
        run.Result(trace).Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void ResolutionFailure_LeavesCollectedDataAvailable()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1) with { Creator = new FailingCreator() };
        var trace = algorithm.Iterations.TracePopulationQuality();
        var run = algorithm.Advised(trace).CreateRun(problem, RandomNumberGenerator.Create(42));
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
        var advised = algorithm.Advised(trace);
        var run = advised.CreateRun(problem, RandomNumberGenerator.Create(42));
        await using (var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                     .GetAsyncEnumerator(TestContext.Current.CancellationToken))
            (await stream.MoveNextAsync()).ShouldBeTrue();
        run.LifecycleState.ShouldBe(RunLifecycleState.Paused);
        run.Result(trace).SampleCount.ShouldBe(1);
        var another = advised.CreateRun(problem, RandomNumberGenerator.Create(43));
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
        var run = algorithm.Advised(trace).CreateRun(problem, RandomNumberGenerator.Create(42));
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

        protected override ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem>
            Wrap<TRunSearchSpace, TRunProblem>(ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> executor,
                IOperator<Creation, RealVector> creator) => executor;

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
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var first = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);
        var second = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);
        var third = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);

        var run = algorithm.Advised(first, second, third).CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(first).By(evaluations).Single().Time.ShouldBe(16);
        run.Result(second).By(evaluations).Single().Time.ShouldBe(16);
        run.Result(third).By(evaluations).Single().Time.ShouldBe(16);
    }

    /// <summary>A counter is an instrument, so each run counts from zero with its own result.</summary>
    [Fact]
    public async Task CandidateClock_CountsEachRunOnItsOwn()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 1);
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var first = algorithm.Evaluator.Invocations.TraceBestSoFar([evaluations]);
        var second = algorithm.Evaluator.Invocations.TraceBestSoFar([evaluations]);

        var firstRun = algorithm.Advised(first).CreateRun(problem, RandomNumberGenerator.Create(42));
        await firstRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var secondRun = algorithm.Advised(second).CreateRun(problem, RandomNumberGenerator.Create(43));
        await secondRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstRun.Result(first).By(evaluations).Single().Time.ShouldBe(16);
        secondRun.Result(second).By(evaluations).Single().Time.ShouldBe(16);
        firstRun.Result(evaluations).ShouldNotBeSameAs(secondRun.Result(evaluations));
    }

    /// <summary>Reusing one analyzer for several runs gives each run an independent trace.</summary>
    [Fact]
    public async Task AnalyzerReuse_CollectsEachRunIndependently()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var trace = algorithm.Iterations.TracePopulationCandidates();
        var advised = algorithm.Advised(trace);
        var firstRun = advised.CreateRun(problem, RandomNumberGenerator.Create(42));
        await firstRun.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var first = firstRun.Result(trace).Snapshot();
        var secondRun = advised.CreateRun(problem, RandomNumberGenerator.Create(43));
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
        var iterations = algorithm.Iterations.Count();
        var quality = algorithm.Iterations.Trace(
            observation => observation.State.Population.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective),
            [iterations]);
        var run = algorithm.Advised(quality).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        run.Result(quality).By(iterations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
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
        var run = algorithm.Advised(firstDimension).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
        var iterations = algorithm.Iterations.Count();
        var offspring = algorithm.Crossover.Invocations.Trace(
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax(),
            [iterations]);
        var run = algorithm.Advised(offspring).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the crossover fires once per later generation.
        run.Result(offspring).SampleCount.ShouldBe(2);

        // Iterations are observed at another boundary, so a sample carries the latest iteration seen before it.
        run.Result(offspring).By(iterations).Select(point => point.Time).ShouldBe([1L, 2L]);
    }

    [Fact]
    public async Task ValueTrace_ReceivesTheTypedCrossoverObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 2);
        var offspringCount = algorithm.Crossover.Invocations.Trace(
            observation => observation.Offspring.Count);
        var run = algorithm.Advised(offspringCount).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
        var run = algorithm.Advised(crossed, mutated).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
            static observation => observation.State.Population.BestMedianWorst(observation.Problem.Objective));

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
        var run = algorithm.Advised(first, second).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.ShouldNotBeSameAs(second);
        run.Result(first).SampleCount.ShouldBe(3);
        run.Result(second).SampleCount.ShouldBe(3);
        run.Result(first).Snapshot().ShouldNotBeSameAs(run.Result(second).Snapshot());
    }

    [Fact]
    public async Task ValueTrace_ComposesForAnEvaluatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var batchSize = algorithm.Evaluator.Invocations.Trace(observation => observation.Candidates.Count, [evaluations]);
        var run = algorithm.Advised(batchSize).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(batchSize).SampleCount.ShouldBeGreaterThan(0);
        run.Result(batchSize).Snapshot().Values.ShouldAllBe(count => count > 0);

        // The clock shares the evaluator boundary, so a sample already counts the candidates it reports.
        run.Result(batchSize).By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    [Fact]
    public async Task CustomFunction_ComposesForAMutatorSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var mutated = algorithm.Mutator.Invocations.Trace(
            observation => observation.Offspring.Select(candidate => candidate[0]).MinMeanMax());
        var run = algorithm.Advised(mutated).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
        var run = algorithm.Advised(kept).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
        var run = algorithm.Advised(removed).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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
        var run = algorithm.Advised(first, second).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));

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

        var run = algorithm.Advised(every).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
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

        var run = algorithm.Advised(everyThird).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
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

        var run = algorithm.Advised(populationSize).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The population size never changes, so only the first firing is kept.
        run.Result(populationSize).SampleCount.ShouldBe(1);
    }

    [Fact]
    public async Task LatestOnlyRetention_KeepsOneEntryCarryingTheMomentOfItsOwnObservation()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var iterations = algorithm.Iterations.Count();
        var current = algorithm.Iterations.Trace(observation => observation.Iteration,
            clocks: [iterations],
            retention: TraceRetention.LatestOnly());

        var run = algorithm.Advised(current).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Every firing replaced the one before it, so the trace holds the last one and its own moment.
        run.Result(current).SampleCount.ShouldBe(1);
        run.Result(current).RequireLatestValue().ShouldBe(6L);
        run.Result(current).By(iterations).Select(point => point.Time).ShouldBe([6L]);
    }

    [Fact]
    public async Task TraceBestCandidateSoFar_RecordsOnlyImprovements()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 6);
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var best = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar([evaluations]);

        var run = algorithm.Advised(best).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var comparer = problem.Objective.TotalOrderComparer;
        var curve = run.Result(best).Snapshot();
        curve.Count.ShouldBeGreaterThan(0);

        // Every recorded entry is strictly better than the one before it, against a rising evaluation count.
        foreach (var (previous, current) in curve.Zip(curve.Skip(1)))
            comparer.Compare(current.Value.ObjectiveVector, previous.Value.ObjectiveVector).ShouldBeLessThan(0);

        run.Result(best).By(evaluations).Select(point => point.Time).ShouldBeInOrder();
    }

    /// <summary>A custom analyzer can install a module for an execution boundary the library does not observe.</summary>
    [Fact]
    public async Task CustomAnalyzer_ObservesABoundaryTheLibraryDoesNotCover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var created = new CreatorCountAnalyzer(algorithm.Creator);

        var run = algorithm.Advised(created).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The creator fills the initial population once, so one firing of 16 candidates.
        created.CreatedCandidates.ShouldBe(16);
    }

    /// <summary>
    /// A clock over a boundary is written by deriving from <see cref="Clock{TTime, TResult}"/> and observing its source
    /// in its aspect, which leaves the clock with counting an observation into its run's result and reading the time.
    /// </summary>
    [Fact]
    public async Task CustomObservingClock_InstallsItselfForItsSource()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var crossoverCalls = new CrossoverCallClock(algorithm.Crossover);
        var quality = algorithm.Iterations.TracePopulationCandidates([crossoverCalls]);

        var run = algorithm.Advised(quality).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The initial population is created rather than crossed, so the first generation is recorded at zero calls.
        run.Result(quality).By(crossoverCalls).Select(point => point.Time).ShouldBe([0L, 1L, 2L]);
    }

    private sealed class CrossoverCallClock(IOperator<Crossover, RealVector> crossover)
        : Clock<long, CountAccumulator>
    {
        protected override CountAccumulator CreateResult(RunResults results) => new();

        protected override long Read(CountAccumulator result) => result.CurrentCount;

        protected override Aspect CreateAspect(CountAccumulator result, RunResults results) =>
            Aspect.Observe(crossover.Invocations, _ => result.IncrementBy(1));
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
        protected override ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem>
            Wrap<TRunSearchSpace, TRunProblem>(ICreationExecutor<RealVector, TRunSearchSpace, TRunProblem> executor,
                IOperator<Creation, RealVector> creator) =>
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

        var run = algorithm.Advised(quality).CreateRun(problem, RandomNumberGenerator.Create(seed: 42));
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
