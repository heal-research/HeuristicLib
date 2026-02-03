using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutation.Mutators;

public class InversionMutator : Mutator<Permutation, PermutationSearchSpace> {
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationSearchSpace searchSpace) {
    var start = random.Integer(parent.Count);
    var end = random.Integer(start, parent.Count);
    var newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
