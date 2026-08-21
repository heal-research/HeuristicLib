using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

/// <summary>
/// Recording a quality curve is a read-only capture at the end of each iteration, so it needs no interceptor.
/// </summary>
public class IterationObservationSpecs
{
    [Fact]
    public async Task QualityCurve_IsTrackedFromTheRun_WithoutAnInterceptor()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var run = algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .TrackBestMedianWorst(out var qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var qualityCurve = run.GetResult(qualityAnalyzer);

        qualityCurve.Count.ShouldBe(5);
        qualityCurve[0].Best.ObjectiveVector.ShouldNotBeNull();
    }

    [Fact]
    public async Task QualityCurve_CanAnchorOnTheAlgorithmExplicitly()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = Analyzer.BestMedianWorst(algorithm);
        var run = algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .WithAnalyzer(qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(qualityAnalyzer).Count.ShouldBe(5);
    }

    [Fact]
    public async Task AnchoringOnAnInnerAlgorithm_ObservesThatInnerLoop()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var innerAlgorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var cycle = CycleAlgorithm.Create(innerAlgorithm) with { MaximumCycles = 2, NewExecutionInstancesPerCycle = false };

        var innerQuality = Analyzer.BestMedianWorst(innerAlgorithm);
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).WithAnalyzer(innerQuality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(innerQuality).Count.ShouldBe(6);
    }

    [Fact]
    public async Task AnalyzerObservesTheStateTheRunStreams()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 4);

        var run = algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .TrackBestMedianWorst(out var qualityAnalyzer);

        var streamedStates = new List<PopulationState<RealVector>>();
        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            streamedStates.Add(state);
        }

        var qualityCurve = run.GetResult(qualityAnalyzer);

        qualityCurve.Count.ShouldBe(streamedStates.Count);
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateGeneticAlgorithm(
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
