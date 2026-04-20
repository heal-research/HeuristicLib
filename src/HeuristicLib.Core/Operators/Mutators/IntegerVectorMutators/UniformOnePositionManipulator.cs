using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;

public record UniformOnePositionManipulator
  : SingleSolutionMutator<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Mutate(IntegerVector parent, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Mutate(random, parent, searchSpace);

  public static IntegerVector Mutate(IRandomNumberGenerator random, IntegerVector vector, IntegerVectorSearchSpace searchSpace)
    => Mutate(random, vector, searchSpace.Minimum, searchSpace.Maximum);

  public static IntegerVector Mutate(IRandomNumberGenerator random, IntegerVector vector, IntegerVector minimum, IntegerVector maximum)
  {
    var index = random.NextInt(0, vector.Count);
    var res = vector.ToArray();
    res[index] = random.NextIntegerVectorUniformAt(minimum, maximum, index);
    return res;
  }
}
