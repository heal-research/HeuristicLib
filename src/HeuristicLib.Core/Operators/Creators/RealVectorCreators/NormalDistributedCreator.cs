using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;

public record NormalDistributedCreator(RealVector Means, RealVector Sigmas)
  : SingleSolutionCreator<RealVector, RealVectorSearchSpace>
{
  public override RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
    => Create(random, searchSpace, Means, Sigmas);

  public static RealVector Create(
    IRandomNumberGenerator random,
    RealVectorSearchSpace searchSpace,
    RealVector means,
    RealVector sigmas)
    => Create(random, searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum);

  public static RealVector Create(
    IRandomNumberGenerator random,
    int length,
    RealVector means,
    RealVector sigmas,
    RealVector minimum,
    RealVector maximum)
  {
    if (!RealVector.AreCompatible(length, means, sigmas, minimum, maximum)) {
      throw new ArgumentException("Vectors must have compatible lengths");
    }

    var value = random.NextGaussian(means, sigmas, length);
    return RealVector.Clamp(value, minimum, maximum);
  }
}
