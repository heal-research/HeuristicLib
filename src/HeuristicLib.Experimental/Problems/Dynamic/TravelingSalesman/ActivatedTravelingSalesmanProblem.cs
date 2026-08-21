using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class ActivatedTravelingSalesmanProblem : DynamicProblem<Permutation, PermutationSearchSpace>
{
    public ActivatedTravelingSalesmanProblem(ITravelingSalesmanProblemData tspData,
                                             IRandomNumberGenerator environmentRandom,
                                             double activationProb = 0.9,
                                             double switchProbability = 0.1,
                                             UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                             int epochLength = int.MaxValue) : base(SingleObjective.Minimize,
        new PermutationSearchSpace(tspData.NumberOfCities), environmentRandom, updatePolicy, epochLength)
    {
        SwitchProbability = switchProbability;
        CurrentState = Generate(tspData, activationProb, environmentRandom);
        ProblemData = tspData;
    }

    public ActivatedTravelingSalesmanProblem(ITravelingSalesmanProblemData tspData,
                                             IRandomNumberGenerator environmentRandom,
                                             bool[] startState,
                                             double switchProbability = 0.1,
                                             UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                             int epochLength = int.MaxValue) : base(SingleObjective.Minimize,
        new PermutationSearchSpace(tspData.NumberOfCities), environmentRandom, updatePolicy, epochLength)
    {
        SwitchProbability = switchProbability;
        ArgumentOutOfRangeException.ThrowIfNotEqual(tspData.NumberOfCities, startState.Length);
        CurrentState = startState.ToArray();
        ProblemData = tspData;
    }

    public IReadOnlyList<bool> CurrentState { get; private set; }
    public double SwitchProbability { get; init; }
    public ITravelingSalesmanProblemData ProblemData { get; }
    public ImmutableArray<int> ActiveCities => CurrentState
                                                .Select((isActive, city) => (isActive, city))
                                                .Where(x => x.isActive)
                                                .Select(x => x.city)
                                                .ToImmutableArray();

    public ITravelingSalesmanProblemData CreateActiveSubproblemData()
    {
        var activeCities = ActiveCities;
        var distances = new double[activeCities.Length, activeCities.Length];

        for (var i = 0; i < activeCities.Length; i++)
        {
            for (var j = 0; j < activeCities.Length; j++)
            {
                distances[i, j] = ProblemData.GetDistance(activeCities[i], activeCities[j]);
            }
        }

        return new TravelingSalesmanDistanceMatrixProblemData(distances);
    }

    public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random,
                                             EvaluationTiming timing)
    {
        return solution
               .Where(x => CurrentState[x])
               .SelectCircularPairs(ProblemData.GetDistance)
               .Sum();
    }

    protected override void Update()
    {
        var active = new List<int>();
        var inactive = new List<int>();

        for (var i = 0; i < CurrentState.Count; i++)
        {
            if (CurrentState[i])
            {
                active.Add(i);
            }
            else
            {
                inactive.Add(i);
            }
        }

        var k = Math.Min(active.Count, inactive.Count);
        if (k == 0)
        {
            return;
        }

        ShuffleInPlace(active, EnvironmentRandom);
        ShuffleInPlace(inactive, EnvironmentRandom);

        var next = CurrentState.ToArray();

        for (var i = 0; i < k; i++)
        {
            if (!EnvironmentRandom.NextBool(SwitchProbability))
            {
                continue;
            }

            var a = active[i];
            var b = inactive[i];
            next[a] = false;
            next[b] = true;
        }

        CurrentState = next;
    }

    // Fisher–Yates shuffle
    private static void ShuffleInPlace<T>(IList<T> list, IRandomNumberGenerator rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.NextInt(0, i, true);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static bool[] Generate(ITravelingSalesmanProblemData tspData, double activationProb, IRandomNumberGenerator random) =>
        Enumerable.Range(0, tspData.NumberOfCities).Select(_ => random.NextBool(activationProb)).ToArray();
}
