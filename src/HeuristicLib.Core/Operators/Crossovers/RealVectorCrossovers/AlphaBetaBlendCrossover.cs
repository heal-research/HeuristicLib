using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;

public record AlphaBetaBlendCrossover : SingleSolutionCrossover<RealVector, RealVectorSearchSpace>
{
  public AlphaBetaBlendCrossover(double alpha = 0.7)
  {
    Alpha = alpha;
  }

  public double Alpha { get; }
  public double Beta => 1 - Alpha;

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
    => Cross(parents.Parent1, parents.Parent2, random, searchSpace, Alpha);

  public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, double alpha)
    => Cross(parent1, parent2, random, alpha, searchSpace.Minimum, searchSpace.Maximum);

  public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, double alpha, RealVector minimum, RealVector maximum)
  {
    var beta = 1 - alpha;
    var result = (alpha * parent1) + (beta * parent2);
    return RealVector.Clamp(result, minimum, maximum);
  }
}
