using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

public class AnalysisSpecs
{
    // One trace observes two algorithms of one run, keeping one best so far across both, which each run creates anew.
    [Fact]
    public async Task OneAnalyzer_CanCombineTwoAlgorithms()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var firstAlgorithm = CreateAlgorithm(problem);
        var secondAlgorithm = CreateAlgorithm(problem);
        var combined = Analyzer.Trace(
            new[] { firstAlgorithm.Iterations, secondAlgorithm.Iterations },
            () => new BestSoFar(),
            (best, observation) => best.Update(observation.State.Population.Select(candidate => candidate.ObjectiveVector), observation.Problem.Objective));
        var both = firstAlgorithm.Then(secondAlgorithm).Advised(combined);

        var first = both.CreateRun(problem, RandomNumberGenerator.Create(42));
        await first.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var second = both.CreateRun(problem, RandomNumberGenerator.Create(43));
        await second.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.Result(combined).SampleCount.ShouldBe(8);
        first.Result(combined).Latest.ShouldNotBeNull();
        second.Result(combined).SampleCount.ShouldBe(8);
    }

    [Fact]
    public async Task PopulationQuality_WithRetentionAndSharedAxes()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var iterations = algorithm.Iterations.Count();
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var quality = algorithm.Iterations.TracePopulationQuality(retention: TraceRetention.EveryNth(2), clocks: [iterations, evaluations]);
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar(clocks: [evaluations]);
        var run = algorithm.Advised(quality, best).CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var snapshot = run.Result(quality).Snapshot();
        snapshot.By(iterations).Select(point => point.Time).ShouldBe([2L, 4L]);
        snapshot.By(evaluations).Count.ShouldBe(2);
        run.Result(best).Latest.ShouldNotBeNull();
    }

    [Fact]
    public async Task ValuesAndRanking_TraceAnAlgorithm()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var size = algorithm.Iterations.Trace(observation => observation.State.Population.EvaluatedCandidates.Count);
        var quality = algorithm.Iterations.Trace(
            static observation => observation.State.Population.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective));

        var run = algorithm.Advised(size, quality).CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(size).Latest!.Value.Value.ShouldBe(16);
        run.Result(quality).SampleCount.ShouldBe(4);
    }

    // Each trial is its own run, so one trace attached to the repeated algorithm gives every trial its own result.
    [Fact]
    public async Task EveryTrial_HasItsOwnTraceAndClockObservingTwoBoundaries()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var quality = algorithm.Iterations.TracePopulationQuality(clocks: [evaluations]);
        var run = algorithm.Advised(quality).Repeat(2).CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Trials[0].Run.Result(quality).ShouldNotBeSameAs(run.Trials[1].Run.Result(quality));
        run.Trials.ShouldAllBe(trial => trial.Run.Result(quality).SampleCount == 4);
        foreach (var trial in run.Trials)
            trial.Run.Result(quality).Snapshot().By(evaluations)[^1].Time.ShouldBe(trial.Run.Result(evaluations).CurrentCount);
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
