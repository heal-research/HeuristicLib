using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public record SwapSingleSolutionMutator : SingleSolutionMutator<Permutation>
{
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random)
  {
    return Permutation.SwapRandomElements(solution, random);
  }
}
