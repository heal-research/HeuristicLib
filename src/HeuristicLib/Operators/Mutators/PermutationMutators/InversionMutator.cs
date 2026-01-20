using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public class InversionMutator : SingleSolutionMutator<Permutation, PermutationSearchSpace> {
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) {
    int start = random.NextInt(parent.Count);
    int end = random.NextInt(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
