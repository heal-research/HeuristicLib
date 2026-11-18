using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

/// <summary>
/// Recording a quality curve is a read-only capture at the end of each iteration, so it needs no interceptor.
/// </summary>
public class IterationObservationSpecs
{
    [Fact]
    public async Task QualityCurve_IsAStatefulAnalysis()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations)]);

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            run.Result(quality).Latest.ShouldNotBeNull();
            var current = run.Result(quality).Snapshot();
            current.Count.ShouldBeGreaterThan(0);
        }

        run.Result(quality).By(iterations.Clock).Count.ShouldBe(3);
    }

    [Fact]
    public async Task QualityCurve_IsTrackedFromTheRun_WithoutAnInterceptor()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [qualityAnalyzer]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var qualityCurve = run.Result(qualityAnalyzer).Snapshot();

        qualityCurve.Count.ShouldBe(5);
        qualityCurve[0].Value.Best.ObjectiveVector.ShouldNotBeNull();
    }

    [Fact]
    public async Task QualityCurve_CanObserveTheAlgorithmExplicitly()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [qualityAnalyzer]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(qualityAnalyzer).SampleCount.ShouldBe(5);
    }

    [Fact]
    public async Task ObservingAnInnerAlgorithm_ObservesThatInnerLoop()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var innerAlgorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var cycle = (CycleAlgorithm.Create(innerAlgorithm) with { MaximumCycles = 2 }).Sharing(innerAlgorithm);

        var innerQuality = innerAlgorithm.Iterations.TracePopulationCandidates();
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [innerQuality]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(innerQuality).SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task AnalyzerObservesTheStateTheRunStreams()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 4);

        var qualityAnalyzer = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [qualityAnalyzer]);

        var streamedStates = new List<PopulationState<RealVector>>();
        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            streamedStates.Add(state);
        }

        var qualityCurve = run.Result(qualityAnalyzer).Snapshot();

        qualityCurve.Count.ShouldBe(streamedStates.Count);
    }

    private static GeneticAlgorithm<RealVector> CreateGeneticAlgorithm(
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
