using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

/// <summary>
/// Performs the rounded blend alpha crossover (BLX-a) of two integer vectors.<br/>
/// It creates new offspring by sampling a new value in the range [min_i - d * alpha, max_i + d * alpha) at each position i
/// Here min_i and max_i are the smaller and larger value of the two parents at position i and d is max_i - min_i.
/// </summary>
public record RoundedBlendAlphaCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public double Alpha
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      field = value;
    }
  } = 0.5;

  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Cross(random, [parents.Parent1, parents.Parent2], searchSpace, Alpha);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IReadOnlyList<IntegerVector> parents,
    IntegerVectorSearchSpace searchSpace,
    double alpha)
    => Cross(random, parents, searchSpace.Minimum, searchSpace.Maximum, alpha);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IReadOnlyList<IntegerVector> parents,
    IntegerVector minimum,
    IntegerVector maximum,
    double alpha)
  {
    var parent1 = parents[0];
    var parent2 = parents[1];

    int length = parent1.Count;
    var result = new int[length];

    for (int i = 0; i < length; i++) {
      int p1 = parent1[i];
      int p2 = parent2[i];

      int minP = Math.Min(p1, p2);
      int maxP = Math.Max(p1, p2);

      // BLX-α real interval
      double d = (maxP - minP) * alpha;
      double minReal = minP - d;
      double maxReal = maxP + d;

      int minBound = minimum[minimum.Count == 1 ? 0 : i];
      int maxBound = maximum[maximum.Count == 1 ? 0 : i];

      // clamp real interval to integer bounds (as reals) &&
      // convert to feasible integer interval (inclusive)
      int lo = RealVector.CeilToInteger(Math.Max(minReal, minBound), minBound, maxBound);
      int hi = RealVector.FloorToInteger(Math.Min(maxReal, maxBound), minBound, maxBound);

      if (lo > hi) {
        // interval narrower than one integer; pick the nearest feasible int
        result[i] = RealVector.RoundToInteger((Math.Max(minReal, minBound) + Math.Min(maxReal, maxBound)) * 0.5, minBound, maxBound);
        continue;
      }

      result[i] = random.NextInt(lo, hi, true);
    }

    return result;
  }
}
