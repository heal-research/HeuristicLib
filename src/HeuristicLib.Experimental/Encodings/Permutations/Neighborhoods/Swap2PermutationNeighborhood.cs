using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public record Swap2PermutationNeighborhood : PermutationNeighborhood<Swap2PermutationNeighborhood.Move>
{
    public readonly record struct Move(int I, int J);

    public override IEnumerable<Move> Moves(Permutation genotype, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, IProblem<Permutation, PermutationSearchSpace> problem)
    {
        for (var i = 0; i < genotype.Count; i++)
            for (var j = 0; j < i; j++)
                yield return new Move(i, j);
    }

    public override Permutation Apply(Permutation genotype, Move move, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, IProblem<Permutation, PermutationSearchSpace> problem)
    {
        var d = genotype.ToArray();
        (d[move.I], d[move.J]) = (d[move.J], d[move.I]);
        return new Permutation(d);
    }
}
