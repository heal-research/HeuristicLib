using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator.Permutations;

public class SwapMutator : Mutator<Permutation> {
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}
