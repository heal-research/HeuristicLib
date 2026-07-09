using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Partial.TSP;

public sealed record AppendCityNeighborhood
    : StatelessIncrementalBoundNeighborhood<Permutation, PermutationSearchSpace, TravelingSalesmanMoveProblem, AppendCityNeighborhood.Move>
{
    public readonly record struct Move(int City);

    public override IEnumerable<Move> Moves(
        Permutation candidate,
        IRandomNumberGenerator random,
        PermutationSearchSpace searchSpace,
        TravelingSalesmanMoveProblem problem)
    {
        var used = candidate.ToHashSet();

        for (var city = 0; city < searchSpace.Length; city++)
            if (!used.Contains(city))
                yield return new Move(city);
    }

    public override bool RandomMove(
        Permutation candidate,
        IRandomNumberGenerator random,
        PermutationSearchSpace searchSpace,
        TravelingSalesmanMoveProblem problem, out Move move)
    {
        var moves = Moves(candidate, random, searchSpace, problem).ToArray();
        if (moves.Length == 0)
        {
            move = default;
            return false;
        }

        move = moves[random.NextInt(moves.Length)];
        return true;
    }

    public override Permutation ApplyMove(
        Permutation candidate,
        Move move,
        PermutationSearchSpace searchSpace,
        TravelingSalesmanMoveProblem problem)
        => new(candidate.Append(move.City).ToArray());

    public override ObjectiveVector BoundIncrement(
        Permutation candidate,
        Move move,
        IRandomNumberGenerator random,
        PermutationSearchSpace searchSpace,
        TravelingSalesmanMoveProblem problem)
    {
        return candidate.Count == 0 ? 0.0 : problem.Distance(candidate[^1], move.City);
    }
}
