using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;

public class AlphaBetaBlendCrossover : SingleSolutionStatelessCrossover<RealVector, RealVectorSearchSpace>
{
  public AlphaBetaBlendCrossover(double alpha = 0.7) {
    Alpha = alpha;
  }

  public double Alpha { get; }
  public double Beta => 1 - Alpha;

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
  {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
    var result = (Alpha * parent1) + (Beta * parent2);
    return RealVector.Clamp(result, searchSpace.Minimum, searchSpace.Maximum);
  }
}
