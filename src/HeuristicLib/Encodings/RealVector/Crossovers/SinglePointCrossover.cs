using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.RealVector.Crossovers;

public class SinglePointCrossover : Crossover<RealVector, RealVectorEncoding> {
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorEncoding encoding) {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
    var crossoverPoint = random.Integer(1, parent1.Count);
    var offspringValues = new double[parent1.Count];
    for (var i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }

    for (var i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }

    return RealVector.Clamp(new RealVector(offspringValues), encoding.Minimum, encoding.Maximum);
  }
}
