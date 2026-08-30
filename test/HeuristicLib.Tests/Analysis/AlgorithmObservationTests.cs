using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AlgorithmObservationTests
{
    private const int PopulationSize = 16;

    [Fact]
    public async Task AlgorithmAnchor_ObservesOncePerYieldedIteration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var observed = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax(),
            algorithm);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        observed.SampleCount.ShouldBe(4);
        observed.Snapshot().ShouldAllBe(sample => sample.Value.Max == PopulationSize);
    }

    [Fact]
    public async Task AlgorithmAnchor_ObservesStateAfterInterception()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var observed = Analyzer.Trace(
            observation => (IReadOnlyList<double>)[observation.State.Population.EvaluatedCandidates.Count],
            Aggregate.MinMeanMax(),
            algorithm);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), observed)
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
        var observed = Analyzer.Trace(
            observation =>
            {
                previousStates.Add(observation.PreviousState);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                return (IReadOnlyList<double>)[observation.Iteration];
            },
            Aggregate.MinMeanMax(),
            algorithm);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        previousStates.Count.ShouldBe(3);
        previousStates[0].ShouldBeNull();
        previousStates[1].ShouldNotBeNull();
        previousStates[2].ShouldNotBeNull();
    }

    [Fact]
    public async Task AlgorithmAnchoredAnalyzer_RecordsOneEntryPerGeneration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var analysis = Analyzer.TraceBestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task AlgorithmAnchoredAnalyzers_ShareOneAnchorWithoutConflict()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.TraceBestMedianWorst(algorithm);
        var second = Analyzer.TraceBestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), first, second);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
    }

    [Fact]
    public async Task AlgorithmAndInterceptorAnchors_CanBeUsedInTheSameRun()
    {
        var problem = CreateProblem();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { Interceptor = interceptor };
        var atIterationEnd = Analyzer.TraceBestMedianWorst(algorithm);
        var atInterceptor = Analyzer.TraceBestMedianWorst(interceptor);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), atIterationEnd, atInterceptor);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        atIterationEnd.SampleCount.ShouldBe(3);
        atInterceptor.SampleCount.ShouldBe(3);
    }

    /// <summary>
    /// Documents the stale-anchor hazard: the anchor is a reference, so a copy is a different anchor.
    /// </summary>
    [Fact]
    public async Task AlgorithmAnchor_IsReferenceIdentity_SoACopyIsNotObserved()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var analysis = Analyzer.TraceBestMedianWorst(algorithm);

        var copy = algorithm with { PopulationSize = PopulationSize * 2 };
        var run = copy.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(0);
    }

    [Fact]
    public async Task TraceBestMedianWorst_AnchorsOnTheAlgorithmItIsGiven()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var analysis = Analyzer.TraceBestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task TraceAnalyzer_IsReadableWhileTheRunStreams()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.TraceBestMedianWorst(algorithm, iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);
        var observedIterations = 0;

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            observedIterations++;
            quality.SampleCount.ShouldBe(observedIterations);
            quality.Latest.ShouldNotBeNull();
            quality.Latest.Value.At(iterations).ShouldBe(observedIterations);
            state.Population.EvaluatedCandidates.ShouldContain(quality.Latest.Value.Value.Best);
            quality.IsCompleted.ShouldBeFalse();
        }

        quality.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task TraceAnalyzer_SnapshotRemainsStableWhileTheRunContinues()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(
            new ObjectiveVectorsMeasurement<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(),
            Aggregate.BestMedianWorst(),
            algorithm,
            iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);

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
        var quality = Analyzer.TraceBestMedianWorst(algorithm, iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var byIteration = quality.By(iterations);
        byIteration.Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        quality.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task TraceAnalyzer_CombinesContextualRetainedAndPulledCoordinates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var elapsed = Clock.FromElapsedTime(TimeProvider.System);
        var quality = Analyzer.TraceBestMedianWorst(algorithm, iterations, evaluations, elapsed);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);

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
    public void TraceBestMedianWorst_AtOneAnchor_StaysTwoIndependentAnalyses()
    {
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();

        var first = Analyzer.TraceBestMedianWorst<RealVector, RealVectorSearchSpace, TestFunctionProblem,
            PopulationState<RealVector>>(interceptor);
        var second = Analyzer.TraceBestMedianWorst<RealVector, RealVectorSearchSpace, TestFunctionProblem,
            PopulationState<RealVector>>(interceptor);

        // A stateful analyzer holds one run's data, so two of them are never interchangeable.
        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(0);
        second.SampleCount.ShouldBe(0);
    }

    private static TestFunctionProblem CreateProblem() => new(new RastriginFunction(dimension: 4));

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
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
