using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

public sealed class TravelingSalesmanMoveProblem
    : SingleSolutionPartialBoundedProblem<TravelingSalesmanMoveProblem, Permutation, PermutationSearchSpace>
{
    private readonly ITravelingSalesmanProblemData data;

    public TravelingSalesmanMoveProblem(ITravelingSalesmanProblemData data)
        : base(SingleObjective.Minimize, new PermutationSearchSpace(data.NumberOfCities))
    {
        this.data = data;
    }

    public override ObjectiveVector Evaluate(Permutation candidate, IRandomNumberGenerator random) =>
        !IsTerminal(candidate, random) ? throw new ArgumentException("A complete tour is required for evaluation.", nameof(candidate)) : TourLength(candidate);

    public override bool IsTerminal(Permutation candidate, IRandomNumberGenerator random) =>
        candidate.Count == data.NumberOfCities;

    public override ObjectiveVector Bound(Permutation candidate, IRandomNumberGenerator random) =>
        PathLength(candidate);

    public override ObjectiveVector EvaluatePartial(Permutation candidate, IRandomNumberGenerator random) =>
        IsTerminal(candidate, random) ? TourLength(candidate) : PathLength(candidate);

    internal double TourLength(Permutation tour)
    {
        var length = PathLength(tour);

        if (tour.Count > 1)
            length += data.GetDistance(tour[^1], tour[0]);
        return length;
    }

    private double PathLength(Permutation path)
    {
        var length = 0.0;
        for (var i = 0; i < path.Count - 1; i++)
            length += data.GetDistance(path[i], path[i + 1]);
        return length;
    }

    internal double Distance(int fromCity, int toCity) => data.GetDistance(fromCity, toCity);
}
