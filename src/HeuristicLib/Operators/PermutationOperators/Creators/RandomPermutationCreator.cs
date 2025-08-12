using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.PermutationOperators;


public class RandomPermutationCreator : Creator<Permutation, PermutationEncoding> 
{
  public override Permutation Create(IExecutionContext<PermutationEncoding> context) {
    int[] elements = Enumerable.Range(0, context.Encoding.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = context.Random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}
