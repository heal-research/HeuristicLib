using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public record RandomPermutationCreator : SingleCandidateCreator<Permutation, PermutationSearchSpace>
{
    public override Permutation CreateCandidate(IRandomNumberGenerator random, PermutationSearchSpace searchSpace) =>
        random.NextPermutation(searchSpace);

    public static Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random) =>
        random.NextPermutation(searchSpace);

    public static Permutation Create(IRandomNumberGenerator random, int length) =>
        random.NextPermutation(length);
}
