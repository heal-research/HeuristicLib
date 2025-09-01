using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators.Mutators;

public class InversionMutator : Mutator<Permutation, PermutationEncoding> {
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationEncoding encoding) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
