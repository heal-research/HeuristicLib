using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public class AlphaBetaBlendCrossover(double alpha = 0.7) : Crossover<RealVector, RealVectorSearchSpace> {
  public double Alpha { get; set; } = alpha;
  public double Beta => 1 - Alpha;

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
    var result = Alpha * parent1 + Beta * parent2;
    return RealVector.Clamp(result, searchSpace.Minimum, searchSpace.Maximum);
  }
}
