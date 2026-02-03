using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Creators;

public class NormalDistributedCreator(RealVector means, RealVector sigmas) : Creator<RealVector, RealVectorSearchSpace> {
  public RealVector Means { get; set; } = means;
  public RealVector Sigmas { get; set; } = sigmas;

  //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

  public override RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) {
    if (!RealVector.AreCompatible(searchSpace.Length, Means, Sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    var value = RealVector.CreateNormal(searchSpace.Length, Means, Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, searchSpace.Minimum, searchSpace.Maximum);
    return value;
  }
}
