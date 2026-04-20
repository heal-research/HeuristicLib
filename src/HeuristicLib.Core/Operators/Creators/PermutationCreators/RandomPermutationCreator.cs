using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.PermutationCreators;

public record RandomPermutationCreator : SingleSolutionCreator<Permutation, PermutationSearchSpace>
{
  public override Permutation Create(IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
    => random.NextPermutation(searchSpace);

  public static Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random)
    => random.NextPermutation(searchSpace);

  public static Permutation Create(IRandomNumberGenerator random, int length)
    => random.NextPermutation(length);
}
