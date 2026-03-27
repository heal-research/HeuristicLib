using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

public record UniformOnePositionManipulator
  : SingleSolutionMutator<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Mutate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Manipulate(random, parent, searchSpace);

  public static IntegerVector Manipulate(IRandomNumberGenerator random, IntegerVector vector, IntegerVectorSearchSpace searchSpace)
  {
    var index = random.NextInt(0, vector.Count);
    var res = vector.ToArray();
    res[index] = searchSpace.UniformRandom(random, index);
    return res;
  }
}
