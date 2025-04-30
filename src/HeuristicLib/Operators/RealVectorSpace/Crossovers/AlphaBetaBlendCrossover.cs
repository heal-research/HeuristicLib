using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class AlphaBetaBlendCrossover : Crossover<RealVector, RealVectorSearchSpace> {
  public double Alpha { get; }
  public double Beta { get; }
  
  public AlphaBetaBlendCrossover(double alpha = 0.7, double beta = 0.3) {
    Alpha = alpha;
    Beta = beta;
  }

  public override AlphaBetaBlendCrossoverInstance CreateInstance() => new AlphaBetaBlendCrossoverInstance(this);
}

public class AlphaBetaBlendCrossoverInstance : CrossoverInstance<RealVector, RealVectorSearchSpace, AlphaBetaBlendCrossover> {
  public AlphaBetaBlendCrossoverInstance(AlphaBetaBlendCrossover parameters) : base(parameters) { }
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Parameters.Alpha * parent1 + Parameters.Beta * parent2;
  }
}
