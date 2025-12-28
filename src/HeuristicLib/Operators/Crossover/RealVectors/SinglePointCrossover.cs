using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossover.RealVectors;

public class SinglePointCrossover : Crossover<RealVector, RealVectorEncoding> {
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
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

    return RealVector.Clamp(new RealVector(offspringValues), encoding.Minimum, encoding.Maximum);
  }
}
