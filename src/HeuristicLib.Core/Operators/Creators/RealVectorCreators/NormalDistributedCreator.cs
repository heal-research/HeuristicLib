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
    => random.NextRealVectorNormal(searchSpace, means, sigmas);

  public static RealVector Create(
    IRandomNumberGenerator random,
    int length,
    RealVector means,
    RealVector sigmas,
    RealVector minimum,
    RealVector maximum)
    => RealVector.Clamp(random.NextRealVectorNormal(means, sigmas, length), minimum, maximum);
}
