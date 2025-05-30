using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.PermutationSpace;

public record class RandomPermutationCreator : Creator<Permutation, PermutationSearchSpace>
{
  public override RandomPermutationCreatorInstance CreateExecution(PermutationSearchSpace searchSpace) {
    return new RandomPermutationCreatorInstance(this, searchSpace);
  }
}

public class RandomPermutationCreatorInstance : CreatorExecution<Permutation, PermutationSearchSpace, RandomPermutationCreator> 
{
  public RandomPermutationCreatorInstance(RandomPermutationCreator parameters, PermutationSearchSpace searchSpace) : base(parameters, searchSpace) {}
  public override Permutation Create(IRandomNumberGenerator random) {
    int[] elements = Enumerable.Range(0, SearchSpace.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}

// public record class RandomPermutationCreator : StatelessCreator<Permutation, PermutationSearchSpace> {
//   public override Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
//     int[] elements = Enumerable.Range(0, searchSpace.Length).ToArray();
//     for (int i = elements.Length - 1; i > 0; i--) {
//       int j = random.Integer(i + 1);
//       (elements[i], elements[j]) = (elements[j], elements[i]);
//     }
//     return new Permutation(elements);
//   }
// }
