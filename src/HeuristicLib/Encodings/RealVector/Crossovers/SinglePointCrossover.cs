using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public class SinglePointCrossover : Crossover<RealVector, RealVectorEncoding> {
  public override RealVector Cross((RealVector, RealVector) parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var (parent1, parent2) = parents;
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
