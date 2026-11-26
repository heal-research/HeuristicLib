#region traveling-salesperson-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
#endregion traveling-salesperson-imports
using System.Globalization;
using System.Text.Json.Nodes;

namespace HEAL.HeuristicLib.Tests.Docs.Examples;

public sealed class TravelingSalespersonDocumentationSpecs
{
    private const double Optimum = 7542;

    [Fact]
    public async Task TheExampleRunPrintsTheQuotedCurveAndDrawsItsTours()
    {
        // The page shows the output of an invariant-culture console. The test output folder holds berlin52.tsp.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region traveling-salesperson-program
        var instance = TsplibTspInstanceProvider.LoadData(
            "berlin52.tsp",
            bestQuality: 7542);
        var problem = new TravelingSalesmanProblem(instance.ToCoordinatesData());

        var algorithm = GeneticAlgorithm.For(
            problem,
            crossover: new EdgeRecombinationCrossover(),
            selector: TournamentSelector.For(problem, tournamentSize: 3),
            populationSize: 300,
            maximumGenerations: 1000,
            mutationRate: 0.25);

        var qualityAnalyzer = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(
            problem, RandomNumberGenerator.Create(seed: 42), [qualityAnalyzer]);

        await run.CompleteAsync();

        var qualityCurve = run.Result(qualityAnalyzer).Snapshot().Values;
        var best = run.Result(qualityAnalyzer).RequireLatestValue().Best;

        Console.WriteLine($"Execution: {instance.Name}");
        Console.WriteLine($"Best tour length: {best.ObjectiveVector[0]:F0}");

        foreach (var (entry, generation) in
                 qualityCurve.Select((entry, index) => (entry, index + 1)))
        {
            if (generation % 100 != 0)
            {
                continue;
            }

            Console.WriteLine(
                $"{generation,4}: " +
                $"best {entry.Best.ObjectiveVector[0],8:F0}  " +
                $"median {entry.Median.ObjectiveVector[0],8:F0}  " +
                $"worst {entry.Worst.ObjectiveVector[0],8:F0}");
        }
        #endregion traveling-salesperson-program
#pragma warning restore xUnit1051

        // The page quotes this output.
        PrintedOutput.Lines().ShouldBe([
            "Execution: berlin52",
            "Best tour length: 7797",
            " 100: best     8283  median     9686  worst    14004",
            " 200: best     7798  median     7926  worst    10318",
            " 300: best     7797  median     7797  worst    10502",
            " 400: best     7797  median     7797  worst     9944",
            " 500: best     7797  median     7797  worst    10108",
            " 600: best     7797  median     7797  worst    10156",
            " 700: best     7797  median     7797  worst    10232",
            " 800: best     7797  median     7797  worst    10310",
            " 900: best     7797  median     7797  worst     9953",
            "1000: best     7797  median     7797  worst    10181"
        ]);

        int[] tourGenerations = [1, 50, 150, 1000];
        var tours = new JsonArray();
        foreach (var generation in tourGenerations)
        {
            var tour = qualityCurve[generation - 1].Best;
            tours.Add(new JsonObject
            {
                ["generation"] = generation,
                ["length"] = Math.Round(tour.ObjectiveVector[0]),
                ["tour"] = FigureData.Numbers(tour.Candidate)
            });
        }

        var coordinates = instance.ToCoordinatesData().Coordinates;
        FigureData.Verify("traveling-salesperson", new JsonObject
        {
            ["optimum"] = Optimum,
            ["cities"] = new JsonArray([.. coordinates.Select(city => (JsonNode?)FigureData.Numbers([city.X, city.Y]))]),
            ["tours"] = tours,
            ["x"] = FigureData.Numbers(Enumerable.Range(1, qualityCurve.Length)),
            ["series"] = new JsonObject
            {
                ["best"] = FigureData.Numbers(qualityCurve.Select(entry => AboveOptimum(entry.Best.ObjectiveVector[0]))),
                ["median"] = FigureData.Numbers(qualityCurve.Select(entry => AboveOptimum(entry.Median.ObjectiveVector[0]))),
                ["worst"] = FigureData.Numbers(qualityCurve.Select(entry => AboveOptimum(entry.Worst.ObjectiveVector[0])))
            }
        });
    }

    private static double AboveOptimum(double length)
    {
        return (length / Optimum - 1) * 100;
    }
}
