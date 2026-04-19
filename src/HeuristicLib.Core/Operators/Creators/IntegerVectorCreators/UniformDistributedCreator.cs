using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators;

public record UniformDistributedCreator : SingleSolutionCreator<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Create(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Create(random, searchSpace.Length, searchSpace.Minimum, searchSpace.Maximum);

  public static IntegerVector Create(IntegerVectorSearchSpace searchSpace, IRandomNumberGenerator random)
    => Create(random, searchSpace.Length, searchSpace.Minimum, searchSpace.Maximum);

  public static IntegerVector Create(IRandomNumberGenerator random, int length, IntegerVector minimum, IntegerVector maximum)
    => random.NextIntVector(minimum, maximum, length);

  public static IntegerVector Create(IRandomNumberGenerator random, int length, int minimum, int maximum)
    => Create(random, length, new IntegerVector(minimum), new IntegerVector(maximum));
}
