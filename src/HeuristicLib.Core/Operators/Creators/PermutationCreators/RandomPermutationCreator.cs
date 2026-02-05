using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.PermutationCreators;

public class RandomPermutationCreator : SingleSolutionStatelessCreator<Permutation, PermutationSearchSpace>
{
  public override Permutation Create(IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
  {
    var elements = Enumerable.Range(0, searchSpace.Length).ToArray();
    for (var i = elements.Length - 1; i > 0; i--) {
      var j = random.NextInt(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }

    return new Permutation(elements);
  }
}
