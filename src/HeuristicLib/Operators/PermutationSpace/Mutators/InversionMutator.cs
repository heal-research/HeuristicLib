using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class InversionMutator : Mutator<Permutation, PermutationSearchSpace> {
  public override InversionMutatorInstance CreateInstance() => new InversionMutatorInstance(this);
}

public class InversionMutatorInstance : MutatorInstance<Permutation, PermutationSearchSpace, InversionMutator> {
  public InversionMutatorInstance(InversionMutator parameters) : base(parameters) { }
  public override Permutation Mutate(Permutation parent, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
