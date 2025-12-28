using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creator.RealVectors;

public class UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null) : Creator<RealVector, RealVectorEncoding> {
  public UniformDistributedCreator(RealVectorEncoding encoding) : this(encoding.Minimum, encoding.Maximum) { }
  public RealVector? Minimum { get; set; } = minimum;
  public RealVector? Maximum { get; set; } = maximum;

  public override RealVector Create(IRandomNumberGenerator random, RealVectorEncoding encoding) {
    if (Minimum is not null && (Minimum < encoding.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    if (Maximum is not null && (Maximum > encoding.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    if (!RealVector.AreCompatible(encoding.Length, Minimum ?? encoding.Minimum, Maximum ?? encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

    return RealVector.CreateUniform(encoding.Length, Minimum ?? encoding.Minimum, Maximum ?? encoding.Maximum, random);
  }
}
