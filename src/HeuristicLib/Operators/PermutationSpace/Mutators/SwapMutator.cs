using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class SwapMutator : Mutator<Permutation, PermutationSearchSpace> {
  public override SwapMutatorInstance CreateInstance() => new SwapMutatorInstance(this);
}

public class SwapMutatorInstance : MutatorInstance<Permutation, PermutationSearchSpace, SwapMutator> {
  public SwapMutatorInstance(SwapMutator parameters) : base(parameters) {}
  public override Permutation Mutate(Permutation solution, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}
