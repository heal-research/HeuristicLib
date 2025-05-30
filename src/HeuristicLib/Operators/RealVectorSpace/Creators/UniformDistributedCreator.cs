using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class UniformDistributedCreator : Creator<RealVector, RealVectorSearchSpace>
{
  public RealVector? Minimum { get; }
  public RealVector? Maximum { get; }

  public UniformDistributedCreator(RealVector? minimum = null, RealVector? maximum = null) {
    Minimum = minimum;
    Maximum = maximum;
  }
  
  public override UniformDistributedCreatorExecution CreateExecution(RealVectorSearchSpace searchSpace) => new UniformDistributedCreatorExecution(this, searchSpace);
}

public class UniformDistributedCreatorExecution : CreatorExecution<RealVector, RealVectorSearchSpace, UniformDistributedCreator> 
{
  public UniformDistributedCreatorExecution(UniformDistributedCreator parameters, RealVectorSearchSpace searchSpace) : base(parameters, searchSpace) {}
  public override RealVector Create(IRandomNumberGenerator random) {
    if (Parameters.Minimum is not null && (Parameters.Minimum < SearchSpace.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to searchSpace minimum values");
    if (Parameters.Maximum is not null && (Parameters.Maximum > SearchSpace.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to searchSpace maximum values");
    if (!RealVector.AreCompatible(SearchSpace.Length, Parameters.Minimum ?? SearchSpace.Minimum, Parameters.Maximum ?? SearchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");

    return RealVector.CreateUniform(SearchSpace.Length, Parameters.Minimum ?? SearchSpace.Minimum, Parameters.Maximum ?? SearchSpace.Maximum, random);
  }
}
