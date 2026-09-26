using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
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
    public async Task ModulesAndAnalyzers_ObserveInAttachmentOrder()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 1);
        var callbacks = new List<string>();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .Attach(new RecordingModule(algorithm, () => callbacks.Add("first module")))
            .Attach(new RecordingAnalyzer(algorithm, () => callbacks.Add("analyzer")))
            .Attach(new RecordingModule(algorithm, () => callbacks.Add("last module")));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        callbacks.ShouldBe(["first module", "analyzer", "last module"]);
    }

    [Fact]
    public async Task QualityCurve_IsAStatefulAnalysis()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = algorithm.TracePopulationCandidates([iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(quality);

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            quality.Latest.ShouldNotBeNull();
            var current = quality.Snapshot();
            current.Count.ShouldBeGreaterThan(0);
        }

        quality.By(iterations).Count.ShouldBe(3);
    }

    [Fact]
    public async Task QualityCurve_IsTrackedFromTheRun_WithoutAnInterceptor()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
                           .TracePopulationCandidates(out var qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var qualityCurve = qualityAnalyzer.Snapshot();

        qualityCurve.Count.ShouldBe(5);
        qualityCurve[0].Value.Best.ObjectiveVector.ShouldNotBeNull();
    }

    [Fact]
    public async Task QualityCurve_CanObserveTheAlgorithmExplicitly()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 5);

        var qualityAnalyzer = algorithm.TracePopulationCandidates();
        var run = algorithm
            .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .Attach(qualityAnalyzer);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        qualityAnalyzer.SampleCount.ShouldBe(5);
    }

    [Fact]
    public async Task ObservingAnInnerAlgorithm_ObservesThatInnerLoop()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var innerAlgorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 3);
        var cycle = CycleAlgorithm.Create(innerAlgorithm) with { MaximumCycles = 2, NewExecutionInstancesPerCycle = false };

        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
                       .TracePopulationCandidates(out var innerQuality, innerAlgorithm);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        innerQuality.SampleCount.ShouldBe(6);
    }

    [Fact]
    public async Task AnalyzerObservesTheStateTheRunStreams()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateGeneticAlgorithm(problem, maximumGenerations: 4);

        var qualityAnalyzer = algorithm.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).Attach(qualityAnalyzer);

        var streamedStates = new List<PopulationState<RealVector>>();
        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            streamedStates.Add(state);
        }

        var qualityCurve = qualityAnalyzer.Snapshot();

        qualityCurve.Count.ShouldBe(streamedStates.Count);
    }

    private sealed class RecordingModule(IAlgorithm<RealVector, PopulationState<RealVector>> source, Action observed)
        : IExecutionModule
    {
        public void Install(ResolutionScopeBuilder builder) => builder.Observe(source, _ => observed());
    }

    private sealed class RecordingAnalyzer(IAlgorithm<RealVector, PopulationState<RealVector>> source, Action observed)
        : IAnalyzer
    {
        public void Install(ResolutionScopeBuilder builder) => builder.Observe(source, _ => observed());
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
