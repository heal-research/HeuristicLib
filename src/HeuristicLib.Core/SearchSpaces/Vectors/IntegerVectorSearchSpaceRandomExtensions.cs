using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public static class IntegerVectorSearchSpaceRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public IntegerVector NextIntegerVectorUniform(IntegerVectorSearchSpace searchSpace)
      => random.NextIntegerVectorUniform(searchSpace.Minimum, searchSpace.Maximum, searchSpace.Length);

    public IntegerVector NextIntegerVectorNormal(IntegerVectorSearchSpace searchSpace, RealVector means, RealVector sigmas)
      => random.NextIntegerVectorNormal(means, sigmas, searchSpace.Minimum, searchSpace.Maximum, searchSpace.Length);
  }
}