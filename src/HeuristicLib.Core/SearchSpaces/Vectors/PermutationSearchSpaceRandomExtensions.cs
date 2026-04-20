using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public static class PermutationSearchSpaceRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public Permutation NextPermutation(PermutationSearchSpace searchSpace)
      => random.NextPermutation(searchSpace.Length);
  }
}