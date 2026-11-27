using System.Text.Json.Nodes;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Home;

public sealed class HomeDocumentationSpecs
{
    [Fact]
    public async Task TheHeroShowsAPopulationConvergingOnTwoDimensionalRastrigin()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 2));
        var algorithm = GeneticAlgorithm.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.5, mutationStrength: 0.3),
            populationSize: 80,
            maximumGenerations: 40,
            mutationRate: 0.3);

        var generations = new JsonArray();
        var best = new List<double>();
        await foreach (var state in algorithm.RunStreaming(
                           problem, RandomNumberGenerator.Create(seed: 7), ct: TestContext.Current.CancellationToken))
        {
            var candidates = state.Population.EvaluatedCandidates;
            generations.Add(FigureData.Numbers(candidates.SelectMany(candidate => new[]
            {
                Math.Round(candidate.Candidate[0], 3),
                Math.Round(candidate.Candidate[1], 3)
            })));
            best.Add(candidates.Min(candidate => candidate.ObjectiveVector[0]));
        }

        // The hero claims the population starts spread out and ends at the global optimum.
        generations.Count.ShouldBe(40);
        best[0].ShouldBeGreaterThan(0.5);
        best[^1].ShouldBeLessThan(0.01);

        FigureData.Write("home-hero", new JsonObject
        {
            ["bound"] = 5.12,
            ["generations"] = generations,
            ["best"] = FigureData.Numbers(best)
        });
    }
}
