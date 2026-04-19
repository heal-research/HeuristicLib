using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators;

public record NormalDistributedCreator(RealVector Means, RealVector Sigmas)
  : SingleSolutionCreator<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Create(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Create(random, searchSpace, Means, Sigmas);

  public static IntegerVector Create(
    IRandomNumberGenerator random,
    IntegerVectorSearchSpace searchSpace,
    RealVector means,
    RealVector sigmas)
    => Create(random, searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum);

  public static IntegerVector Create(
    IRandomNumberGenerator random,
    int length,
    RealVector means,
    RealVector sigmas,
    IntegerVector minimum,
    IntegerVector maximum)
  {
    if (!RealVector.AreCompatible(length, means, sigmas)) {
      throw new ArgumentException("Vectors must have compatible lengths");
    }

    var value = random.NextGaussian(means, sigmas, length);
    return value.RoundToIntegerVector(minimum, maximum);
  }
}
