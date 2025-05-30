using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;


public record class SinglePointCrossover : Crossover<RealVector, RealVectorSearchSpace>

{
  public override SinglePointCrossoverExecution CreateExecution(RealVectorSearchSpace searchSpace) => new SinglePointCrossoverExecution(this, searchSpace);
}

public class SinglePointCrossoverExecution : CrossoverExecution<RealVector, RealVectorSearchSpace, SinglePointCrossover> 
{
  public SinglePointCrossoverExecution(SinglePointCrossover parameters, RealVectorSearchSpace searchSpace) : base(parameters, searchSpace) { }
  public override RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random) {
    int crossoverPoint = random.Integer(1, parent1.Count);
    double[] offspringValues = new double[parent1.Count];
    for (int i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }
    for (int i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }
    return RealVector.Clamp(new RealVector(offspringValues), SearchSpace.Minimum, SearchSpace.Maximum);
  }
}
