using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public static class PermutationSearchSpaceRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public Permutation NextPermutation(PermutationSearchSpace searchSpace) =>
            random.NextPermutation(searchSpace.Length);
    }
}
