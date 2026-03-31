using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

/// <summary>
/// Single point crossover for integer vectors.
/// </summary>
/// <remarks>
/// It is implemented as described in Michalewicz, Z. 1999. Genetic Algorithms + Data Structures = Evolution Programs. Third, Revised and Extended Edition, Spring-Verlag Berlin Heidelberg.
/// </remarks>
public record SinglePointCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    var parent1 = parents.Parent1;
    var parent2 = parents.Parent2;
    var crossoverPoint = random.NextInt(1, parent1.Count);
    var offspringValues = new int[parent1.Count];
    for (var i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }

    for (var i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }

    return offspringValues;
  }
}
