using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators;

public class SwapMutator : Mutator<Permutation, Encodings.PermutationEncoding>
{
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random, PermutationEncoding encoding) {
    return Permutation.SwapRandomElements(solution, random);
  }
}
