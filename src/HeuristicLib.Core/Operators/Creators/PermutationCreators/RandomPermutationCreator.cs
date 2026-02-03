using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creators.PermutationCreators;

public class RandomPermutationCreator : Creator<Permutation, PermutationSearchSpace>
{
  public override Permutation Create(IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
  {
    var elements = Enumerable.Range(0, searchSpace.Length).ToArray();
    for (var i = elements.Length - 1; i > 0; i--) {
      var j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }

    return new Permutation(elements);
  }
}
