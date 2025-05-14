using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

// public record class RandomPermutationCreator : Creator<Permutation, PermutationSearchSpace> {
//   public override RandomPermutationCreatorInstance CreateInstance() => new RandomPermutationCreatorInstance(this);
// }
//
// public class RandomPermutationCreatorInstance : CreatorInstance<Permutation, PermutationSearchSpace, RandomPermutationCreator> {
//   public RandomPermutationCreatorInstance(RandomPermutationCreator parameters) : base(parameters) {}
//   public override Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
//     int[] elements = Enumerable.Range(0, searchSpace.Length).ToArray();
//     for (int i = elements.Length - 1; i > 0; i--) {
//       int j = random.Integer(i + 1);
//       (elements[i], elements[j]) = (elements[j], elements[i]);
//     }
//     return new Permutation(elements);
//   }
// }

public record class RandomPermutationCreator : StatelessCreator<Permutation, PermutationSearchSpace> {
  public override Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    int[] elements = Enumerable.Range(0, searchSpace.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}
