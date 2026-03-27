using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators;

public record NormalDistributedCreator(RealVector means, RealVector sigmas)
  : SingleSolutionCreator<IntegerVector, IntegerVectorSearchSpace>
{
  public RealVector Means { get; set; } = means;
  public RealVector Sigmas { get; set; } = sigmas;

  // if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

  public override IntegerVector Create(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    var value = RealVector.CreateNormal(searchSpace.Length, Means, Sigmas, random);
    return searchSpace.RoundFeasible(value);
  }
}
