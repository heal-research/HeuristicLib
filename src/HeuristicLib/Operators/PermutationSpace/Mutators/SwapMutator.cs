using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class SwapMutator : Mutator<Permutation, PermutationSearchSpace>
{
  public override SwapMutatorExecution CreateExecution(PermutationSearchSpace searchSpace) => new SwapMutatorExecution(this, searchSpace);
}

public class SwapMutatorExecution : MutatorExecution<Permutation, PermutationSearchSpace, SwapMutator>
{
  public SwapMutatorExecution(SwapMutator parameters, PermutationSearchSpace searchSpace) : base(parameters, searchSpace) {}
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}
