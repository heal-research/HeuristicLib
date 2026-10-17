using System.Text.Json;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using SHA256 = System.Security.Cryptography.SHA256;

namespace HEAL.HeuristicLib.Tests.Algorithms.Fixtures;

internal static class OperatorCutoverScenarios
{
    public static readonly string[] Cases = ["recommendedDefaults", "defaults", "seed7", "seed42", "rate0", "rate1", "rateNaN", "zeroLimit", "negativeLimit", "unlimited"];

    public static async Task<string> CaptureAsync(string scenario)
    {
        if (scenario == "recommendedDefaults")
            return await CaptureRecommendedDefaultsAsync();
        var problem = FuncProblem.Create(static (int value) => (double)(value * value), Space.Instance, SingleObjective.Minimize);
        var algorithm = new GeneticAlgorithm<int>
        {
            Creator = new Creator(),
            Crossover = SelectFirstParentCrossover<int>.Instance,
            Mutator = new Mutator()
        };
        if (scenario != "defaults")
        {
            algorithm = algorithm with
            {
                PopulationSize = 12,
                Elites = 2,
                MaximumGenerations = scenario switch { "zeroLimit" => 0, "negativeLimit" => -2, "unlimited" => null, _ => 6 },
                MutationRate = scenario switch { "rate0" => 0, "rate1" => 1, "rateNaN" => double.NaN, _ => 0.35 }
            };
        }
        var states = new List<int[][]>();
        await foreach (var state in algorithm.Stream(problem, RandomNumberGenerator.Create(scenario == "seed42" ? 42 : 7)))
        {
            states.Add(state.Population.EvaluatedCandidates.Select(value => new[] { value.Candidate, (int)value.ObjectiveVector[0] }).ToArray());
            if (states.Count == (scenario is "defaults" or "unlimited" ? 4 : 6))
                break;
        }
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(states)));
    }

    private static async Task<string> CaptureRecommendedDefaultsAsync()
    {
        var problem = FuncProblem.Create(static (Permutation candidate) =>
            (double)candidate.Select((value, index) => Math.Abs(value - index)).Sum(),
            new PermutationSearchSpace(8), SingleObjective.Minimize);
        var algorithm = GeneticAlgorithm.For(problem);
        var states = new List<int[][]>();
        await foreach (var state in algorithm.Stream(problem, RandomNumberGenerator.Create(7)))
        {
            states.Add(state.Population.EvaluatedCandidates.Select(value =>
                value.Candidate.Append((int)value.ObjectiveVector[0]).ToArray()).ToArray());
            if (states.Count == 6)
                break;
        }
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(states)));
    }

    private sealed class Space : ISearchSpace<int>
    {
        public static readonly Space Instance = new();
        public bool Contains(int candidate) => true;
    }

    private sealed record Creator : SingleCandidateCreator<int>
    {
        public override int CreateCandidate(IRandomNumberGenerator random) => random.NextInt() % 41 - 20;
    }

    private sealed record Mutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent + random.NextInt() % 7 - 3;
    }
}
