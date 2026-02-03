using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public class SwapMutator : Mutator<Permutation>
{
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random) => Permutation.SwapRandomElements(solution, random);
}
