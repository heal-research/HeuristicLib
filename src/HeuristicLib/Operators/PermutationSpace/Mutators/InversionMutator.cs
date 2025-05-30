using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class InversionMutator : Mutator<Permutation, PermutationSearchSpace> 
{
  public override InversionMutatorExecution CreateExecution(PermutationSearchSpace searchSpace) => new InversionMutatorExecution(this, searchSpace);
}

public class InversionMutatorExecution: MutatorExecution<Permutation, PermutationSearchSpace, InversionMutator>
{
  public InversionMutatorExecution(InversionMutator parameters, PermutationSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
