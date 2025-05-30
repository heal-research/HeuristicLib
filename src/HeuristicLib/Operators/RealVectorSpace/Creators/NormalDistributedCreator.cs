using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class NormalDistributedCreator : Creator<RealVector, RealVectorSearchSpace>
{
  public RealVector Means { get; }
  public RealVector Sigmas { get; }

  //public const double DefaultMeans = 0.0;
  //public const double DefaultSigmas = 1.0;

  public NormalDistributedCreator(RealVector means, RealVector sigmas) {
    //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    Means = means;
    Sigmas = sigmas;
  }
  
  public override NormalDistributedCreatorExecution CreateExecution(RealVectorSearchSpace searchSpace) => new NormalDistributedCreatorExecution(this, searchSpace);
}

public class NormalDistributedCreatorExecution : CreatorExecution<RealVector, RealVectorSearchSpace, NormalDistributedCreator> 
{
  public NormalDistributedCreatorExecution(NormalDistributedCreator parameters, RealVectorSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override RealVector Create(IRandomNumberGenerator random) {
    if (!RealVector.AreCompatible(SearchSpace.Length, Parameters.Means, Parameters.Sigmas, SearchSpace.Minimum, SearchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    RealVector value = RealVector.CreateNormal(SearchSpace.Length, Parameters.Means, Parameters.Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, SearchSpace.Minimum, SearchSpace.Maximum);
    return value;
  }
}
