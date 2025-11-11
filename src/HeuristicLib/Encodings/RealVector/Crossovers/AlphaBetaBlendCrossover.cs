using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public class AlphaBetaBlendCrossover(double alpha = 0.7) : Crossover<RealVector, RealVectorEncoding> {
  public double Alpha { get; set; } = alpha;
  public double Beta => 1 - Alpha;

  public override RealVector Cross((RealVector, RealVector) parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var (parent1, parent2) = parents;
    var result = Alpha * parent1 + Beta * parent2;
    return RealVector.Clamp(result, encoding.Minimum, encoding.Maximum);
  }
}
