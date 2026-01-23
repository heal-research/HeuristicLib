using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public class InversionMutator : SingleSolutionMutator<Permutation, PermutationSearchSpace>
{
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
  {
    var start = random.NextInt(parent.Count);
    var end = random.NextInt(start, parent.Count);
    var newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
