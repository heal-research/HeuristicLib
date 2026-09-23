using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

public class AnalysisSpecs
{
    [Fact]
    public async Task OneAnalyzer_CanCombineTwoAlgorithms()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var firstAlgorithm = CreateAlgorithm(problem);
        var secondAlgorithm = CreateAlgorithm(problem);
        var combined = Analyzer.Trace(
            [firstAlgorithm, secondAlgorithm],
            new ObjectiveVectorsMeasurement<RealVector, PopulationState<RealVector>>(),
            Aggregate.BestSoFar());

        await firstAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAnalyzer(combined)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        await secondAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(43)).AddAnalyzer(combined)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        combined.SampleCount.ShouldBe(8);
        combined.Latest.ShouldNotBeNull();
    }

    [Fact]
    public async Task PopulationQuality_WithRetentionAndSharedAxes()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var iterations = Clock.FromIterations(algorithm);
        var evaluations = Clock.FromEvaluations(algorithm.Evaluator);
        var quality = algorithm.TracePopulationQuality(retention: TraceRetention.EveryNth(2), clocks: [iterations, evaluations]);
        var best = algorithm.Evaluator.TraceBestSoFar(clocks: [evaluations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42))
            .AddAnalyzer(quality)
            .AddAnalyzer(best);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var snapshot = quality.Snapshot();
        snapshot.By(iterations).Select(point => point.Time).ShouldBe([2L, 4L]);
        snapshot.By(evaluations).Count.ShouldBe(2);
        best.Latest.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScalarAndNamedMeasurements_InferTheirTypes()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var size = Analyzer.Trace(algorithm, value: observation => observation.State.Population.EvaluatedCandidates.Count);
        var quality = Analyzer.Trace(algorithm, Measurement.ObjectiveVectors(algorithm), Aggregate.BestMedianWorst());

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42)).AddAnalyzer(size).AddAnalyzer(quality)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        size.Latest!.Value.Value.ShouldBe(16);
        quality.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task TrialFactory_CreatesItsOwnClocksAndObservesTwoBoundaries()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var quality = TrialAnalyzer.Create(
            (GeneticAlgorithm<RealVector> trial) =>
                trial.TracePopulationQuality(clocks: [Clock.FromEvaluations(trial.Evaluator)]));
        var run = algorithm.Repeat(2).CreateRun(problem, RandomNumberGenerator.Create(42)).AddTrialAnalyzer(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var results = run.GetAnalyzers(quality);
        results[0].Analyzer.ShouldNotBeSameAs(results[1].Analyzer);
        results.ShouldAllBe(result => result.Analyzer.SampleCount == 4);
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = 16,
            MaximumGenerations = 4,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(0.2, 0.15),
            Selector = TournamentSelector.For(problem, 2),
            Elites = 1
        };
}
