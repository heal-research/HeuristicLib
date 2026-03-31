using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedLocalCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    return Apply(random, parents.Parent1, parents.Parent2, searchSpace);
  }

  public static IntegerVector Apply(
    IRandomNumberGenerator random,
    IntegerVector parent1,
    IntegerVector parent2,
    IntegerVectorSearchSpace searchSpace)
  {
    if (parent1.Count != parent2.Count)
      throw new ArgumentException("Parents must have same length.", nameof(parent1));

    int length = parent1.Count;
    var result = new int[length];
    for (int i = 0; i < length; i++) {
      double factor = random.NextDouble();
      double x = factor * parent1[i] + (1.0 - factor) * parent2[i];
      result[i] = searchSpace.RoundFeasible(x, i);
    }

    return result;
  }
}
