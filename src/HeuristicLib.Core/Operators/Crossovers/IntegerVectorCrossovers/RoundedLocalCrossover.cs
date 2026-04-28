using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedLocalCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Cross(random, parents.Parent1, parents.Parent2, searchSpace);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IntegerVector parent1,
    IntegerVector parent2,
    IntegerVectorSearchSpace searchSpace)
    => Cross(random, parent1, parent2, searchSpace.Minimum, searchSpace.Maximum);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IntegerVector parent1,
    IntegerVector parent2,
    IntegerVector minimum,
    IntegerVector maximum)
  {
    if (parent1.Count != parent2.Count)
      throw new ArgumentException("Parents must have same length.", nameof(parent1));

    int length = parent1.Count;
    var result = new int[length];
    for (int i = 0; i < length; i++) {
      double factor = random.NextDouble();
      double value = factor * parent1[i] + (1.0 - factor) * parent2[i];
      result[i] = RealVector.RoundToIntegerAt(value, minimum, maximum, i);
    }

    return result;
  }
}
