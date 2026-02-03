using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;

public class NormalDistributedCreator(RealVector means, RealVector sigmas) : Creator<RealVector, RealVectorSearchSpace>
{
  public RealVector Means { get; set; } = means;
  public RealVector Sigmas { get; set; } = sigmas;

  //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

  public override RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
  {
    if (!RealVector.AreCompatible(searchSpace.Length, Means, Sigmas, searchSpace.Minimum, searchSpace.Maximum)) {
      throw new ArgumentException("Vectors must have compatible lengths");
    }
    var value = RealVector.CreateNormal(searchSpace.Length, Means, Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, searchSpace.Minimum, searchSpace.Maximum);

    return value;
  }
}
