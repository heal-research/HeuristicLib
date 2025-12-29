using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;

public class SinglePointCrossover : Crossover<RealVector, RealVectorSearchSpace> {
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
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
