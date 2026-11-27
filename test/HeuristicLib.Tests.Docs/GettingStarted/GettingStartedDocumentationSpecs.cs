using System.Text.Json.Nodes;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.GettingStarted;

public sealed class GettingStartedDocumentationSpecs
{
    [Fact]
    public async Task AFirstAnalysisRecordsTheBestSoFarCurveOfTheFirstOptimizer()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = GeneticAlgorithm.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            populationSize: 200,
            maximumGenerations: 500,
            mutationRate: 0.2);
        var finalState = await algorithm.RunToCompletionAsync(
            problem, RandomNumberGenerator.Create(seed: 123), ct: TestContext.Current.CancellationToken);
        var finalBest = finalState.Population.EvaluatedCandidates
            .Min(candidate => candidate.ObjectiveVector[0]);
        var printed = new List<double>();

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region getting-started-best-so-far
        var bestSoFar = Analyzer.BestSoFar<RealVector>();
        var run = algorithm.CreateRun(
            problem,
            RandomNumberGenerator.Create(seed: 123),
            [bestSoFar]);

        await run.CompleteAsync();

        var curve = run.Result(bestSoFar);
        Console.WriteLine($"Improvements: {curve.Count}");

        foreach (var budget in new[] { 1_000, 10_000, 50_000 })
        {
            var point = curve.Last(entry => entry.Evaluations <= budget);
            Console.WriteLine($"Best within {budget,6} evaluations: {point.Value[0]:F8}");
        }

        var last = curve.Last();
        Console.WriteLine($"Best reached at {last.Evaluations} evaluations: {last.Value[0]:F8}");
        #endregion getting-started-best-so-far
#pragma warning restore xUnit1051

        foreach (var budget in new[] { 1_000, 10_000, 50_000 })
            printed.Add(curve.Last(entry => entry.Evaluations <= budget).Value[0]);

        // The curve covers every evaluated candidate, including offspring that replacement
        // discarded, so it can only match or beat the best member of the final population.
        curve.Count.ShouldBeGreaterThan(1);
        curve.Select(entry => entry.Value[0]).ShouldBeInOrder(SortDirection.Descending);
        printed.ShouldBeInOrder(SortDirection.Descending);
        last.Value[0].ShouldBeLessThanOrEqualTo(finalBest);
    }

    [Fact]
    public async Task TheProgressFigureShowsEveryGenerationOfTheStreamedRun()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = GeneticAlgorithm.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            populationSize: 200,
            maximumGenerations: 500,
            mutationRate: 0.2);

        var generations = new List<int>();
        var best = new List<double>();
        var median = new List<double>();
        var worst = new List<double>();
        await foreach (var state in algorithm.RunStreaming(
                           problem, RandomNumberGenerator.Create(seed: 123), ct: TestContext.Current.CancellationToken))
        {
            var values = state.Population.EvaluatedCandidates
                .Select(candidate => candidate.ObjectiveVector[0])
                .Order()
                .ToArray();
            generations.Add(generations.Count + 1);
            best.Add(values[0]);
            median.Add(values[values.Length / 2]);
            worst.Add(values[^1]);
        }

        // The page prints the best objective of every hundredth generation.
        double[] printed = [0.00034423, 0.00001563, 0.00000121, 0.00000014, 0.00000004];
        for (var i = 0; i < printed.Length; i++)
            Math.Round(best[(i + 1) * 100 - 1], 8).ShouldBe(printed[i]);

        FigureData.Write("getting-started-convergence", new JsonObject
        {
            ["x"] = FigureData.Numbers(generations),
            ["series"] = new JsonObject
            {
                ["best"] = FigureData.Numbers(best),
                ["median"] = FigureData.Numbers(median),
                ["worst"] = FigureData.Numbers(worst)
            }
        });
    }
}
