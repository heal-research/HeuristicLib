using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.IntegerVectorCrossovers;

/// <summary>
/// Performs the rounded blend alpha-beta crossover (BLX-a-b) of two integer vectors.<br/>
/// At each position it samples a new value from the interval spanned by both parents, widened by <see cref="Alpha"/>
/// towards the better parent and by <see cref="Beta"/> towards the worse one, then rounds and clamps it to the search
/// space bounds.
/// </summary>
public record RoundedBlendAlphaBetaCrossover : SingleCandidateCrossover<IntegerVector, IntegerVectorSearchSpace>
{
    /// <summary>
    /// Widens the sampling interval beyond the better parent, as a fraction of the distance between the parents.
    /// Larger values explore further away from the better parent; zero samples only between the parents.
    /// </summary>
    /// <remarks>
    /// A negative value narrows the interval instead of widening it, and a non-finite value collapses it. Both cases
    /// fall back to the nearest feasible integer rather than failing.
    /// </remarks>
    public double Alpha { get; init; } = 0.75;

    /// <summary>
    /// Widens the sampling interval beyond the worse parent, as a fraction of the distance between the parents.
    /// It is usually smaller than <see cref="Alpha"/> so the offspring leans towards the better parent.
    /// </summary>
    /// <remarks>
    /// A negative value narrows the interval instead of widening it, and a non-finite value collapses it. Both cases
    /// fall back to the nearest feasible integer rather than failing.
    /// </remarks>
    public double Beta { get; init; } = 0.25;

    public override IntegerVector CrossParents(Parents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Cross(random, parents.Parent1, parents.Parent2, searchSpace, Alpha, Beta);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector betterParent, IntegerVector worseParent, IntegerVectorSearchSpace searchSpace, double alpha, double beta) =>
        Cross(random, betterParent, worseParent, searchSpace.Minimum, searchSpace.Maximum, alpha, beta);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector betterParent, IntegerVector worseParent, IntegerVector minimum, IntegerVector maximum, double alpha, double beta)
    {
        int length = betterParent.Count;
        var result = new int[length];

        for (int i = 0; i < length; i++)
        {
            int bp = betterParent[i];
            int wp = worseParent[i];

            double d = Math.Abs(bp - wp);

            // Asymmetric BLX-α-β real interval
            double minReal, maxReal;
            if (bp <= wp)
            {
                minReal = bp - d * alpha; // extend below better by α
                maxReal = wp + d * beta; // extend above worse by β
            }
            else
            {
                minReal = wp - d * beta; // extend below worse by β
                maxReal = bp + d * alpha; // extend above better by α
            }

            int minBound = minimum[minimum.Count == 1 ? 0 : i];
            int maxBound = maximum[maximum.Count == 1 ? 0 : i];

            int lo = RealVector.CeilToInteger(minReal, minBound, maxBound);
            int hi = RealVector.FloorToInteger(maxReal, minBound, maxBound);

            if (lo > hi)
            {
                // no feasible integer point inside the interval after clamping
                result[i] = RealVector.RoundToInteger((minReal + maxReal) * 0.5, minBound, maxBound);
                continue;
            }

            // Uniform integer sampling in [lo, hi]
            result[i] = random.NextInt(lo, hi, true);
        }

        return IntegerVector.FromOwnedArray(result);
    }
}
