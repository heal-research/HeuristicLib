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
    => random.NextIntegerVectorNormal(searchSpace, means, sigmas);

  public static IntegerVector Create(
    IRandomNumberGenerator random,
    int length,
    RealVector means,
    RealVector sigmas,
    IntegerVector minimum,
    IntegerVector maximum)
    => random.NextIntegerVectorNormal(means, sigmas, minimum, maximum, length);
}
