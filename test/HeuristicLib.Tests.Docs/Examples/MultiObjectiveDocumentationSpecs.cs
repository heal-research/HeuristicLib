#region multi-objective-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
#endregion multi-objective-imports
using System.Globalization;
using System.Text.Json.Nodes;

namespace HEAL.HeuristicLib.Tests.Docs.Examples;

public sealed class MultiObjectiveDocumentationSpecs
{
    [Fact]
    public async Task TheExampleRunPrintsTheQuotedFrontAndDrawsIt()
    {
        // The page shows the output of an invariant-culture console.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        #region multi-objective-problem
        var searchSpace = new BoundedRealVectorSearchSpace(
            length: 2,
            minimum: [2.0, 2.0],
            maximum: [12.0, 25.0]);

        var problem = new FuncProblem<RealVector, BoundedRealVectorSearchSpace>(
            (RealVector candidate) =>
            {
                var radius = candidate[0];
                var height = candidate[1];

                var material = 2 * Math.PI * radius * radius + 2 * Math.PI * radius * height;
                var volume = Math.PI * radius * radius * height;

                return new[] { material, volume };
            },
            searchSpace,
            MultiObjective.Create(ObjectiveDirection.Minimize, ObjectiveDirection.Maximize));
        #endregion multi-objective-problem

        #region multi-objective-algorithm
        var algorithm = NSGA2.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            selector: ParetoCrowdingTournamentSelector.For(problem, dominateOnEqualities: false, tournamentSize: 2),
            replacer: ParetoCrowdingReplacer.For(problem, dominateOnEqualities: false),
            populationSize: 100,
            maximumGenerations: 200);
        #endregion multi-objective-algorithm

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region multi-objective-front
        var finalState = await algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(seed: 5));

        var front = ParetoFront.ExtractFrom(finalState.Population.EvaluatedCandidates, problem.Objective)
            .OrderBy(candidate => candidate.ObjectiveVector[0])
            .ToList();

        Console.WriteLine($"Nondominated: {front.Count} of {finalState.Population.EvaluatedCandidates.Count}");
        Console.WriteLine();
        Console.WriteLine(" material    volume   radius   height    h/r");

        foreach (var candidate in front.Where((_, index) => index % 12 == 0))
        {
            var radius = candidate.Candidate[0];
            var height = candidate.Candidate[1];

            Console.WriteLine(
                $"{candidate.ObjectiveVector[0],9:F1}{candidate.ObjectiveVector[1],10:F1}" +
                $"{radius,9:F2}{height,9:F2}{height / radius,7:F2}");
        }
        #endregion multi-objective-front
#pragma warning restore xUnit1051

        #region multi-objective-choice
        var choice = front.First(candidate => candidate.ObjectiveVector[1] >= 2000);

        Console.WriteLine(
            $"Cheapest design holding at least 2000: " +
            $"radius {choice.Candidate[0]:F2}, height {choice.Candidate[1]:F2}, " +
            $"material {choice.ObjectiveVector[0]:F1}");
        #endregion multi-objective-choice

        // The page quotes this output.
        PrintedOutput.Lines().ShouldBe([
            "Nondominated: 100 of 100",
            "",
            " material    volume   radius   height    h/r",
            "     89.7      63.3     2.48     3.28   1.32",
            "    455.5     745.7     5.03     9.38   1.86",
            "    743.2    1555.5     6.31    12.44   1.97",
            "   1126.7    2903.4     7.67    15.70   2.05",
            "   1524.2    4567.2     8.86    18.53   2.09",
            "   1828.9    6001.6     9.66    20.45   2.12",
            "   2134.8    7567.7    10.41    22.23   2.14",
            "   2343.0    8700.3    10.89    23.37   2.15",
            "   2518.4    9694.2    11.27    24.29   2.15",
            "Cheapest design holding at least 2000: radius 6.89, height 13.85, material 898.5"
        ]);

        // The same seed again, generation by generation, for the initial population and the hypervolume curve. The
        // curve accumulates the nondominated designs of every population, so it never decreases.
        IReadOnlyList<EvaluatedCandidate<RealVector>>? initial = null;
        (double Material, double Volume)[] archive = [];
        var hyperVolume = new List<double>();
        await foreach (var state in algorithm.RunStreaming(
                           problem, RandomNumberGenerator.Create(seed: 5), ct: TestContext.Current.CancellationToken))
        {
            initial ??= state.Population.EvaluatedCandidates;
            archive = Nondominated([
                .. archive,
                .. state.Population.EvaluatedCandidates.Select(design => (design.ObjectiveVector[0], design.ObjectiveVector[1]))
            ]);
            hyperVolume.Add(HyperVolume(archive));
        }

        FigureData.Verify("multi-objective", new JsonObject
        {
            ["initial"] = Designs(initial!),
            ["front"] = Designs(front),
            ["choice"] = front.IndexOf(choice),
            ["generations"] = FigureData.Numbers(Enumerable.Range(1, hyperVolume.Count)),
            ["hyperVolume"] = FigureData.Numbers(hyperVolume.Select(value => value / 1e6))
        });
    }

    // The designs that no other design beats on both less material and more volume. Sorted by material, each kept
    // design holds more than every cheaper one.
    private static (double Material, double Volume)[] Nondominated(IEnumerable<(double Material, double Volume)> designs)
    {
        var kept = new List<(double Material, double Volume)>();
        foreach (var design in designs.OrderBy(design => design.Material).ThenByDescending(design => design.Volume))
        {
            if (kept.Count == 0 || design.Volume > kept[^1].Volume)
            {
                kept.Add(design);
            }
        }

        return [.. kept];
    }

    // The area between the nondominated designs and the reference point (3000, 0) that some design dominates.
    private static double HyperVolume((double Material, double Volume)[] front)
    {
        var area = 0.0;
        var previousVolume = 0.0;
        foreach (var (material, volume) in front)
        {
            area += (3000 - material) * (volume - previousVolume);
            previousVolume = volume;
        }

        return area;
    }

    // Each design as material, volume, radius and height.
    private static JsonArray Designs(IEnumerable<EvaluatedCandidate<RealVector>> candidates)
    {
        return new JsonArray([.. candidates.Select(candidate => (JsonNode?)FigureData.Numbers([
            candidate.ObjectiveVector[0],
            candidate.ObjectiveVector[1],
            candidate.Candidate[0],
            candidate.Candidate[1]
        ]))]);
    }
}
