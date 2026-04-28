using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;

public record SinglePointCrossover : SingleSolutionCrossover<RealVector, RealVectorSearchSpace>
{
  public override RealVector Cross(IParents<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace)
    => Cross(parents.Parent1, parents.Parent2, random, searchSpace);

  public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, int? crossoverPoint = null)
    => Cross(parent1, parent2, random, searchSpace.Minimum, searchSpace.Maximum, crossoverPoint);

  public static RealVector Cross(RealVector parent1, RealVector parent2, IRandomNumberGenerator random, RealVector minimum, RealVector maximum, int? crossoverPoint = null)
  {
    var cutPoint = crossoverPoint ?? random.NextInt(1, parent1.Count);
    var offspringValues = new double[parent1.Count];
    for (var i = 0; i < cutPoint; i++) {
      offspringValues[i] = parent1[i];
    }

    for (var i = cutPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }

    return RealVector.Clamp(new RealVector(offspringValues), minimum, maximum);
  }
}
