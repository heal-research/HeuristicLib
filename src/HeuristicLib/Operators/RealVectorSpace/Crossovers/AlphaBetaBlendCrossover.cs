using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;

public record class AlphaBetaBlendCrossover : Crossover<RealVector, RealVectorSearchSpace>
{
  public double Alpha { get; }
  public double Beta { get; }
  
  public AlphaBetaBlendCrossover(double alpha = 0.7, double beta = 0.3) {
    Alpha = alpha;
    Beta = beta;
  }

  public override AlphaBetaBlendCrossoverExecution CreateExecution(RealVectorSearchSpace searchSpace) => new AlphaBetaBlendCrossoverExecution(this, searchSpace);
}

public class AlphaBetaBlendCrossoverExecution : CrossoverExecution<RealVector, RealVectorSearchSpace, AlphaBetaBlendCrossover> 
{
  public AlphaBetaBlendCrossoverExecution(AlphaBetaBlendCrossover parameters, RealVectorSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random) {
    return Parameters.Alpha * parent1 + Parameters.Beta * parent2;
  }
}
