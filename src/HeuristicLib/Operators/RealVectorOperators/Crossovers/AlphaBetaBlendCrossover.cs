using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.RealVectorOperators;

public class AlphaBetaBlendCrossover : Crossover<RealVector, RealVectorEncoding>
{
  public double Alpha { get; set; }
  public double Beta { get; set; }
  
  public AlphaBetaBlendCrossover(double alpha = 0.7, double beta = 0.3) {
    Alpha = alpha;
    Beta = beta;
  }

  public override RealVector Cross((RealVector, RealVector) parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var (parent1, parent2) = parents;
    var result = Alpha * parent1 + Beta * parent2;
    return RealVector.Clamp(result, encoding.Minimum, encoding.Maximum);
  }
}
