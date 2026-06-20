using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Partial.TSP;

public sealed class TravelingSalesmanMoveProblem
    : SingleSolutionPartialBoundedProblem<Permutation, PermutationSearchSpace>
{
    private readonly ITravelingSalesmanProblemData data;

    public TravelingSalesmanMoveProblem(ITravelingSalesmanProblemData data)
        : base(SingleObjective.Minimize, new PermutationSearchSpace(data.NumberOfCities)) => this.data = data;

    public override ObjectiveVector Evaluate(Permutation genotype, IRandomNumberGenerator random)
        => !IsTerminal(genotype, random) ? throw new ArgumentException("A complete tour is required for evaluation.", nameof(genotype)) : TourLength(genotype);

    public override bool IsTerminal(Permutation genotype, IRandomNumberGenerator random)
        => genotype.Count == data.NumberOfCities;

    public override ObjectiveVector Bound(Permutation genotype, IRandomNumberGenerator random)
        => PathLength(genotype);

    public override ObjectiveVector EvaluatePartial(Permutation genotype, IRandomNumberGenerator random)
        => IsTerminal(genotype, random) ? TourLength(genotype) : PathLength(genotype);

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
