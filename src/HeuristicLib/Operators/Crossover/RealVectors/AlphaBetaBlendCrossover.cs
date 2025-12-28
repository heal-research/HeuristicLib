using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossover.RealVectors;

public class AlphaBetaBlendCrossover(double alpha = 0.7) : Crossover<RealVector, RealVectorEncoding> {
  public double Alpha { get; set; } = alpha;
  public double Beta => 1 - Alpha;

  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
    var result = Alpha * parent1 + Beta * parent2;
    return RealVector.Clamp(result, encoding.Minimum, encoding.Maximum);
  }
}
