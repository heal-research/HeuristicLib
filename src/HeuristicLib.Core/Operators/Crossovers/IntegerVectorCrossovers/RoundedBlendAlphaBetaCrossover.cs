using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

public record RoundedBlendAlphaBetaCrossover : SingleSolutionCrossover<IntegerVector, IntegerVectorSearchSpace>
{
  public double Alpha
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      field = value;
    }
  } = 0.75;

  public double Beta
  {
    get;
    init {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      field = value;
    }
  } = 0.25;

  public override IntegerVector Cross(IParents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
    => Cross(random, parents.Parent1, parents.Parent2, searchSpace, Alpha, Beta);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IntegerVector betterParent,
    IntegerVector worseParent,
    IntegerVectorSearchSpace searchSpace,
    double alpha,
    double beta)
    => Cross(random, betterParent, worseParent, searchSpace.Minimum, searchSpace.Maximum, alpha, beta);

  public static IntegerVector Cross(
    IRandomNumberGenerator random,
    IntegerVector betterParent,
    IntegerVector worseParent,
    IntegerVector minimum,
    IntegerVector maximum,
    double alpha,
    double beta)
  {
    if (betterParent.Count != worseParent.Count)
      throw new ArgumentException("Parents must have same length.", nameof(betterParent));
    if (alpha < 0)
      throw new ArgumentOutOfRangeException(nameof(alpha), "alpha must be >= 0.");
    if (beta < 0)
      throw new ArgumentOutOfRangeException(nameof(beta), "beta must be >= 0.");

    int length = betterParent.Count;
    var result = new int[length];

    for (int i = 0; i < length; i++) {
      int bp = betterParent[i];
      int wp = worseParent[i];

      double d = Math.Abs(bp - wp);

      // Asymmetric BLX-α-β real interval
      double minReal, maxReal;
      if (bp <= wp) {
        minReal = bp - d * alpha; // extend below better by α
        maxReal = wp + d * beta; // extend above worse by β
      } else {
        minReal = wp - d * beta; // extend below worse by β
        maxReal = bp + d * alpha; // extend above better by α
      }

      int minBound = minimum[minimum.Count == 1 ? 0 : i];
      int maxBound = maximum[maximum.Count == 1 ? 0 : i];

      int lo = RealVector.CeilToInteger(minReal, minBound, maxBound);
      int hi = RealVector.FloorToInteger(maxReal, minBound, maxBound);

      if (lo > hi) {
        // no feasible integer point inside the interval after clamping
        result[i] = RealVector.RoundToInteger((minReal + maxReal) * 0.5, minBound, maxBound);
        continue;
      }

      // Uniform integer sampling in [lo, hi]
      result[i] = random.NextInt(lo, hi, true);
    }

    return result; // auto-converts to IntegerVector in your setup
  }
}
