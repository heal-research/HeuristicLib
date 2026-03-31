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
    => Apply(random, parents.Parent1, parents.Parent2, searchSpace, Alpha, Beta);

  public static IntegerVector Apply(
    IRandomNumberGenerator random,
    IntegerVector betterParent,
    IntegerVector worseParent,
    IntegerVectorSearchSpace searchSpace,
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

      int lo = searchSpace.CeilingFeasible(minReal, i);
      int hi = searchSpace.FloorFeasible(maxReal, i);

      if (lo > hi) {
        // no feasible point inside (can happen with steps / tight bounds)
        result[i] = searchSpace.RoundFeasible((minReal + maxReal) * 0.5, i);
        continue;
      }

      // Uniform integer sampling in [lo, hi]
      result[i] = random.NextInt(lo, hi, true);
    }

    return result; // auto-converts to IntegerVector in your setup
  }
}
