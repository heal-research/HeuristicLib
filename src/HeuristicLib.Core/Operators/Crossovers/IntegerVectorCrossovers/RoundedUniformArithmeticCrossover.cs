using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedUniformArithmeticCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public double Alpha
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);
      field = value;
    }
  } = 0.5;
  public double Probability
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1);
      field = value;
    }
  } = 1;

  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    return Apply(random, parents.Parent1, parents.Parent2, searchSpace, Alpha, Probability);
  }

  public static IntegerVector Apply(
    IRandomNumberGenerator random,
    IntegerVector parent1,
    IntegerVector parent2,
    IntegerVectorSearchSpace searchSpace,
    double alpha,
    double probability)
  {
    int length = parent1.Count;

    if (length != parent2.Count)
      throw new ArgumentException("Parents must have same length.", nameof(parent1));
    if (alpha < 0 || alpha > 1)
      throw new ArgumentOutOfRangeException(nameof(alpha), "alpha must be in [0,1].");
    if (probability < 0 || probability > 1)
      throw new ArgumentOutOfRangeException(nameof(probability), "probability must be in [0,1].");

    var result = new int[length];

    for (int i = 0; i < length; i++) {
      if (random.NextDouble() < probability) {
        double x = alpha * parent1[i] + (1.0 - alpha) * parent2[i];
        result[i] = searchSpace.RoundFeasible(x, i);
      } else {
        result[i] = parent1[i];
      }
    }

    return result;
  }
}
