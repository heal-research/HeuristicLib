using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public record PermutationConstructionNeighborhood : PermutationNeighborhood<int>
{
    public override IEnumerable<int> Moves(Permutation genotype, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, IProblem<Permutation, PermutationSearchSpace> problem) =>
        Enumerable.Range(0, searchSpace.Length).Except(genotype);

    public override Permutation Apply(Permutation genotype, int move, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, IProblem<Permutation, PermutationSearchSpace> problem) => new(genotype.Append(move));
}
