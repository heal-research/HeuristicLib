using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class UniformDistributedCreator : Creator<RealVector, RealVectorSearchSpace> {
  public RealVector? Minimum { get; }
  public RealVector? Maximum { get; }

  public UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null) {
    Minimum = minimum;
    Maximum = maximum;
  }
  
  public override UniformDistributedCreatorInstance CreateInstance() => new UniformDistributedCreatorInstance(this);
}

public class UniformDistributedCreatorInstance : CreatorInstance<RealVector, RealVectorSearchSpace, UniformDistributedCreator> {
  public UniformDistributedCreatorInstance(UniformDistributedCreator parameters) : base(parameters) {}
  public override RealVector Create(RealVectorSearchSpace searchSpace, IRandomNumberGenerator random) {
    if (Parameters.Minimum is not null && (Parameters.Minimum < searchSpace.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    if (Parameters.Maximum is not null && (Parameters.Maximum > searchSpace.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    if (!RealVector.AreCompatible(searchSpace.Length, Parameters.Minimum ?? searchSpace.Minimum, Parameters.Maximum ?? searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

    return RealVector.CreateUniform(searchSpace.Length, Parameters.Minimum ?? searchSpace.Minimum, Parameters.Maximum ?? searchSpace.Maximum, random);
  }
}
