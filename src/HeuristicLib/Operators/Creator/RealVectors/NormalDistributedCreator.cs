using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creator.RealVectors;

public class NormalDistributedCreator(RealVector means, RealVector sigmas) : Creator<RealVector, RealVectorEncoding> {
  public RealVector Means { get; set; } = means;
  public RealVector Sigmas { get; set; } = sigmas;

  //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

  public override RealVector Create(IRandomNumberGenerator random, RealVectorEncoding encoding) {
    if (!RealVector.AreCompatible(encoding.Length, Means, Sigmas, encoding.Minimum, encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    RealVector value = RealVector.CreateNormal(encoding.Length, Means, Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, encoding.Minimum, encoding.Maximum);
    return value;
  }
}
