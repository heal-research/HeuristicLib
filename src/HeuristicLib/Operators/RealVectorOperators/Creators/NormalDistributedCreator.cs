using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators.Creators;

public class NormalDistributedCreator : Creator<RealVector, RealVectorEncoding> {
  public RealVector Means { get; set; }
  public RealVector Sigmas { get; set; }

  public NormalDistributedCreator(RealVector means, RealVector sigmas) {
    //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    Means = means;
    Sigmas = sigmas;
  }

  public override RealVector Create(IRandomNumberGenerator random, RealVectorEncoding encoding) {
    if (!RealVector.AreCompatible(encoding.Length, Means, Sigmas, encoding.Minimum, encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    RealVector value = RealVector.CreateNormal(encoding.Length, Means, Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, encoding.Minimum, encoding.Maximum);
    return value;
  }
}
