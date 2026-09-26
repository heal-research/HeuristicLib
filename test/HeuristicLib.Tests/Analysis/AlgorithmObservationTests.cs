using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AlgorithmObservationTests
{
    private const int PopulationSize = 16;

    [Fact]
    public async Task AlgorithmObservation_ObservesOncePerYieldedIteration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var observed = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        observed.SampleCount.ShouldBe(4);
        observed.Snapshot().ShouldAllBe(sample => sample.Value.Max == PopulationSize);
    }

    [Fact]
    public async Task AlgorithmObservation_ObservesStateAfterInterception()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var observed = Analyzer.Trace(algorithm,
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        observed.SampleCount.ShouldBe(4);
        observed.Snapshot().ShouldAllBe(sample => sample.Value.Max == 5);
    }

    [Fact]
    public async Task AlgorithmObservation_CarriesThePreviousStateAndTheProblem()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var previousStates = new List<PopulationState<RealVector>?>();
        var observed = Analyzer.Trace(algorithm,
            observation =>
            {
                previousStates.Add(observation.PreviousState);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                return (IReadOnlyList<double>)[observation.Iteration];
            },
            Aggregate.MinMeanMax());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        previousStates.Count.ShouldBe(3);
        previousStates[0].ShouldBeNull();
        previousStates[1].ShouldNotBeNull();
        previousStates[2].ShouldNotBeNull();
    }

    [Fact]
    public async Task AlgorithmAnalyzer_RecordsOneEntryPerGeneration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var analysis = algorithm.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task AlgorithmAnalyzers_ShareOneSourceWithoutConflict()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.TracePopulationCandidates();
        var second = algorithm.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(first).Attach(second);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
    }

    [Fact]
    public async Task AlgorithmAndInterceptorSources_CanBeUsedInTheSameRun()
    {
        var problem = CreateProblem();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { Interceptor = interceptor };
        var atIterationEnd = algorithm.TracePopulationCandidates();
        var atInterceptor = interceptor.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(atIterationEnd).Attach(atInterceptor);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        atIterationEnd.SampleCount.ShouldBe(3);
        atInterceptor.SampleCount.ShouldBe(3);
    }

    /// <summary>
    /// The analyzer observes the exact algorithm configuration supplied as its source.
    /// </summary>
    [Fact]
    public async Task AlgorithmObservation_UsesTheConfiguredSourceInstance()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var analysis = algorithm.TracePopulationCandidates();

        var copy = algorithm with { PopulationSize = PopulationSize * 2 };
        var run = copy.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(0);
    }

    [Fact]
    public async Task TraceAnalyzer_IsReadableWhileTheRunStreams()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var iterations = Clock.FromIterations(algorithm);
        var quality = algorithm.TracePopulationCandidates([iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);
        var observedIterations = 0;

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            observedIterations++;
            quality.SampleCount.ShouldBe(observedIterations);
            quality.Latest.ShouldNotBeNull();
            quality.Latest.Value.At(iterations).ShouldBe(observedIterations);
            state.Population.EvaluatedCandidates.ShouldContain(quality.Latest.Value.Value.Best);
        }

    }

    [Fact]
    public async Task TraceAnalyzer_SnapshotRemainsStableWhileTheRunContinues()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(algorithm,
            new ObjectiveVectorsMeasurement<RealVector, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst(),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await using var enumerator = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                        .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        var firstIteration = quality.Snapshot();

        (await enumerator.MoveNextAsync()).ShouldBeTrue();

        firstIteration.Count.ShouldBe(1);
        firstIteration[0].At(iterations).ShouldBe(1);
        quality.SampleCount.ShouldBe(2);
        quality.Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public async Task TraceAnalyzer_ByIterationPublishesAStableProjection()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = algorithm.TracePopulationCandidates([iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var byIteration = quality.By(iterations);
        byIteration.Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task TraceAnalyzer_CombinesContextualRetainedAndPulledCoordinates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var elapsed = Clock.FromElapsedTime(TimeProvider.System);
        var quality = algorithm.TracePopulationCandidates([iterations, evaluations, elapsed]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        quality.By(iterations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);

        var evaluationCounts = quality.By(evaluations).Select(point => point.Time).ToArray();
        evaluationCounts.ShouldAllBe(count => count > 0);
        evaluationCounts.ShouldBe(evaluationCounts.Order());

        var elapsedTimes = quality.By(elapsed).Select(point => point.Time).ToArray();
        elapsedTimes.ShouldAllBe(time => time >= TimeSpan.Zero);
        elapsedTimes.ShouldBe(elapsedTimes.Order());
    }

    [Fact]
    public void TracePopulationCandidates_AtOneSource_StaysTwoIndependentAnalyzers()
    {
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();

        var first = interceptor.TracePopulationCandidates();
        var second = interceptor.TracePopulationCandidates();

        // A stateful analyzer holds one run's data, so two of them are never interchangeable.
        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(0);
        second.SampleCount.ShouldBe(0);
    }

    /// <summary>
    /// A cycle that recreates its execution instances restarts the observed algorithm, so its iteration count restarts
    /// with it. Reusing the instances continues one algorithm, so the count continues.
    /// </summary>
    [Theory]
    [InlineData(true, new long[] { 1, 2, 3, 1, 2, 3 })]
    [InlineData(false, new long[] { 1, 2, 3, 4, 5, 6 })]
    public async Task IterationClock_FollowsWhetherTheCycleRecreatesItsInstances(
        bool newExecutionInstancesPerCycle, long[] expectedIterations)
    {
        var problem = CreateProblem();
        var inner = CreateAlgorithm(problem, maximumGenerations: 3);
        var cycle = CycleAlgorithm.Create(inner) with
        {
            MaximumCycles = 2,
            NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle
        };

        var iterations = Clock.FromIterations(inner);
        var quality = inner.TracePopulationCandidates([iterations]);
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        quality.By(iterations).Select(point => point.Time).ShouldBe(expectedIterations);
    }

    private static TestFunctionProblem CreateProblem() => new(new RastriginFunction(dimension: 4));

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(
        TestFunctionProblem problem, int maximumGenerations) =>
        new()
        {
            PopulationSize = PopulationSize,
            MaximumGenerations = maximumGenerations,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1
        };

    private sealed record TruncatingInterceptor(int KeptCandidates)
        : StatelessInterceptor<RealVector, PopulationState<RealVector>>
    {
        public override PopulationState<RealVector> Transform(PopulationState<RealVector> currentState,
                                                              PopulationState<RealVector>? previousState,
                                                              IRandomNumberGenerator random) =>
            currentState with { Population = Population.From(currentState.Population.Take(KeptCandidates)) };
    }
}
