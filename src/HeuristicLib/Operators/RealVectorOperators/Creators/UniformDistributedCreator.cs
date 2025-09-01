using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators.Creators;

public class UniformDistributedCreator : Creator<RealVector, RealVectorEncoding> {
  public RealVector? Minimum { get; set; }
  public RealVector? Maximum { get; set; }

  public UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null) {
    Minimum = minimum;
    Maximum = maximum;
  }

  public override RealVector Create(IRandomNumberGenerator random, RealVectorEncoding encoding) {
    if (Minimum is not null && (Minimum < encoding.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    if (Maximum is not null && (Maximum > encoding.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    if (!RealVector.AreCompatible(encoding.Length, Minimum ?? encoding.Minimum, Maximum ?? encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

    return RealVector.CreateUniform(encoding.Length, Minimum ?? encoding.Minimum, Maximum ?? encoding.Maximum, random);
  }
}
