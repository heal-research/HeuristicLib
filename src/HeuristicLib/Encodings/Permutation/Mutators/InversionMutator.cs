using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutation.Mutators;

public class InversionMutator : Mutator<Permutation, PermutationEncoding> {
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationEncoding encoding) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
