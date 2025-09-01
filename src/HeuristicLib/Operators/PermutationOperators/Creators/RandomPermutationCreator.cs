using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators.Creators;

public class RandomPermutationCreator : Creator<Permutation, PermutationEncoding> {
  public override Permutation Create(IRandomNumberGenerator random, PermutationEncoding encoding) {
    int[] elements = Enumerable.Range(0, encoding.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }

    return new Permutation(elements);
  }
}
