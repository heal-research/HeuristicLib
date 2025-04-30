using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class NormalDistributedCreator : Creator<RealVector, RealVectorSearchSpace> {
  public RealVector Means { get; }
  public RealVector Sigmas { get; }

  //public const double DefaultMeans = 0.0;
  //public const double DefaultSigmas = 1.0;

  public NormalDistributedCreator(RealVector means, RealVector sigmas) {
    //if (!RealVector.AreCompatible(searchSpace.Length, means, sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    Means = means;
    Sigmas = sigmas;
  }
  
  public override NormalDistributedCreatorInstance CreateInstance() => new NormalDistributedCreatorInstance(this);
}

public class NormalDistributedCreatorInstance : CreatorInstance<RealVector, RealVectorSearchSpace, NormalDistributedCreator> {
  public NormalDistributedCreatorInstance(NormalDistributedCreator parameters) : base(parameters) { }
  public override RealVector Create(RealVectorSearchSpace searchSpace, IRandomNumberGenerator random) {
    if (!RealVector.AreCompatible(searchSpace.Length, Parameters.Means, Parameters.Sigmas, searchSpace.Minimum, searchSpace.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    RealVector value = RealVector.CreateNormal(searchSpace.Length, Parameters.Means, Parameters.Sigmas, random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, searchSpace.Minimum, searchSpace.Maximum);
    return value;
  }
}
