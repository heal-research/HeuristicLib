using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AlgorithmObservationTests
{
    private const int PopulationSize = 16;

    [Fact]
    public void ObservableAlgorithm_NotifiesOncePerYieldedIteration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var observedCounts = new List<int>();

        algorithm.ObserveWith(state => observedCounts.Add(state.Population.EvaluatedCandidates.Count))
                 .Complete(problem, RandomNumberGenerator.Create(seed: 42),
                     ct: TestContext.Current.CancellationToken);

        observedCounts.Count.ShouldBe(4);
        observedCounts.ShouldAllBe(count => count == PopulationSize);
    }

    [Fact]
    public void ObservableAlgorithm_ObservesStateAfterInterception()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var observedCounts = new List<int>();

        algorithm.ObserveWith(state => observedCounts.Add(state.Population.EvaluatedCandidates.Count))
                 .Complete(problem, RandomNumberGenerator.Create(seed: 42),
                     ct: TestContext.Current.CancellationToken);

        observedCounts.Count.ShouldBe(4);
        observedCounts.ShouldAllBe(count => count == 5);
    }

    [Fact]
    public void ObservableAlgorithm_PassesPreviousStateAndProblem()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var previousStates = new List<PopulationState<RealVector>?>();

        algorithm.ObserveWith((_, previousState, searchSpace, observedProblem) =>
                 {
                     previousStates.Add(previousState);
                     searchSpace.ShouldBeSameAs(problem.SearchSpace);
                     observedProblem.ShouldBeSameAs(problem);
                 })
                 .Complete(problem, RandomNumberGenerator.Create(seed: 42),
                     ct: TestContext.Current.CancellationToken);

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
        var analysis = Analyzer.BestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).WithAnalyzer(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(analysis).Count.ShouldBe(4);
    }

    [Fact]
    public async Task AlgorithmAnchoredAnalyzers_ShareOneAnchorWithoutConflict()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = Analyzer.BestMedianWorst(algorithm);
        var second = Analyzer.BestMedianWorst(algorithm);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).WithAnalyzers(first, second);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(first).Count.ShouldBe(3);
        run.GetResult(second).Count.ShouldBe(3);
    }

    [Fact]
    public async Task AlgorithmAndInterceptorAnchors_CanBeUsedInTheSameRun()
    {
        var problem = CreateProblem();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { Interceptor = interceptor };
        var atIterationEnd = Analyzer.BestMedianWorst(algorithm);
        var atInterceptor = Analyzer.BestMedianWorst(interceptor);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
                           .WithAnalyzers(atIterationEnd, atInterceptor);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(atIterationEnd).Count.ShouldBe(3);
        run.GetResult(atInterceptor).Count.ShouldBe(3);
    }

    /// <summary>
    /// Documents the stale-anchor hazard: the anchor is a reference, so a copy is a different anchor.
    /// </summary>
    [Fact]
    public async Task AlgorithmAnchor_IsReferenceIdentity_SoACopyIsNotObserved()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var analysis = Analyzer.BestMedianWorst(algorithm);

        var copy = algorithm with { PopulationSize = PopulationSize * 2 };
        var run = copy.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).WithAnalyzer(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(analysis).ShouldBeEmpty();
    }

    [Fact]
    public async Task TrackBestMedianWorst_TakesTheAnchorFromTheRun()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
                           .TrackBestMedianWorst(out var analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(analysis).Count.ShouldBe(4);
    }

    [Fact]
    public void BestMedianWorstAnalysis_ComparesAnchorCollectionsStructurally()
    {
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();

        var first = Analyzer.BestMedianWorst<RealVector, RealVectorSearchSpace, TestFunctionProblem,
            PopulationState<RealVector>>(interceptor);
        var second = Analyzer.BestMedianWorst<RealVector, RealVectorSearchSpace, TestFunctionProblem,
            PopulationState<RealVector>>(interceptor);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void BestMedianWorstAnalysis_WithDifferentAnchors_IsNotEqual()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();

        var atAlgorithm = Analyzer.BestMedianWorst(algorithm);
        var atInterceptor = Analyzer.BestMedianWorst<RealVector, RealVectorSearchSpace, TestFunctionProblem,
            PopulationState<RealVector>>(interceptor);

        atAlgorithm.ShouldNotBe(atInterceptor);
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
