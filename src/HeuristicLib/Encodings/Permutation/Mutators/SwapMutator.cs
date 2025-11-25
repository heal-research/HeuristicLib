using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutation.Mutators;

public class SwapMutator : Mutator<Permutation> {
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}
