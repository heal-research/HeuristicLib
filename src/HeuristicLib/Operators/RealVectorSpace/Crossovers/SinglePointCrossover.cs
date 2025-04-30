using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.RealVectorSpace;



public record class SinglePointCrossover : Crossover<RealVector, RealVectorSearchSpace> {
  public override SinglePointCrossoverInstance CreateInstance() => new SinglePointCrossoverInstance(this);
}

public class SinglePointCrossoverInstance : CrossoverInstance<RealVector, RealVectorSearchSpace, SinglePointCrossover> {
  public SinglePointCrossoverInstance(SinglePointCrossover parameters) : base(parameters) { }
  public override RealVector Cross(RealVector parent1, RealVector parent2, RealVectorSearchSpace searchSpace, IRandomNumberGenerator random) {
    int crossoverPoint = random.Integer(1, parent1.Count);
    double[] offspringValues = new double[parent1.Count];
    for (int i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }
    for (int i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }
    return RealVector.Clamp(new RealVector(offspringValues), searchSpace.Minimum, searchSpace.Maximum);
  }
}
