using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creator.RealVectors;

public class UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null) : Creator<RealVector, RealVectorSearchSpace> {
  public UniformDistributedCreator(RealVectorSearchSpace searchSpace) : this(searchSpace.Minimum, searchSpace.Maximum) { }
  public RealVector? Minimum { get; set; } = minimum;
  public RealVector? Maximum { get; set; } = maximum;

  public override RealVector Create(IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) {
    if (Minimum is not null && (Minimum < searchSpace.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    if (Maximum is not null && (Maximum > searchSpace.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    if (!RealVector.AreCompatible(searchSpace.Length, Minimum ?? searchSpace.Minimum, Maximum ?? searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

    return RealVector.CreateUniform(searchSpace.Length, Minimum ?? searchSpace.Minimum, Maximum ?? searchSpace.Maximum, random);
  }
}
