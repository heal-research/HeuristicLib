using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

/// <summary>
/// Recording a quality curve is a read-only capture at the end of each iteration, so it needs no interceptor.
/// </summary>
public class IterationObservationSpecs
{
    [Fact]
    public async Task QualityCurve_IsAStatefulRunBoundAnalysis()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.TraceBestMedianWorst(algorithm, iterations);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), quality);

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            quality.Latest.ShouldNotBeNull();
            var current = quality.Snapshot();
            current.Count.ShouldBeGreaterThan(0);
        }

        quality.By(iterations).Count.ShouldBe(3);
        quality.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task QualityCurve_IsTrackedFromTheRun_WithoutAnInterceptor()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = Analyzer.TraceBestMedianWorst(algorithm);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var qualityCurve = qualityAnalyzer.Snapshot();

        qualityCurve.Count.ShouldBe(5);
        qualityCurve[0].Value.Best.ObjectiveVector.ShouldNotBeNull();
    }

    [Fact]
    public async Task QualityCurve_CanAnchorOnTheAlgorithmExplicitly()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = Analyzer.TraceBestMedianWorst(algorithm);
        var run = algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42), qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        qualityAnalyzer.SampleCount.ShouldBe(5);
    }

    [Fact]
    public async Task AnchoringOnAnInnerAlgorithm_ObservesThatInnerLoop()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var innerAlgorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var cycle = CycleAlgorithm.Create(innerAlgorithm) with { MaximumCycles = 2, NewExecutionInstancesPerCycle = false };

        var innerQuality = Analyzer.TraceBestMedianWorst(innerAlgorithm);
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), innerQuality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        innerQuality.SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task AnalyzerObservesTheStateTheRunStreams()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 4);

        var qualityAnalyzer = Analyzer.TraceBestMedianWorst(algorithm);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), qualityAnalyzer);

        var streamedStates = new List<PopulationState<RealVector>>();
        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            streamedStates.Add(state);
        }

        var qualityCurve = qualityAnalyzer.Snapshot();

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
