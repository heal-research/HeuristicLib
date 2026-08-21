using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

/// <summary>
/// Blends two integer vectors position by position. Each position is crossed with probability
/// <see cref="Probability"/>; a crossed position becomes the <see cref="Alpha"/> weighted average of both parents,
/// rounded and clamped to the search space bounds, while an uncrossed position keeps the first parent's value.
/// </summary>
public record RoundedUniformArithmeticCrossover : SingleCandidateCrossover<IntegerVector, IntegerVectorSearchSpace>
{
    /// <summary>
    /// Weight of the first parent in the blend, normally in <c>[0,1]</c>. The second parent receives the complementary
    /// weight, so <c>0.5</c> is the midpoint and <c>1</c> keeps the first parent's value.
    /// </summary>
    /// <remarks>
    /// A value outside <c>[0,1]</c> extrapolates beyond the parents instead of interpolating between them, and the
    /// result is still clamped to the search space bounds.
    /// </remarks>
    public double Alpha { get; init; } = 0.5;

    /// <summary>
    /// Probability of blending an individual position, normally in <c>[0,1]</c>. The default of <c>1</c> blends every
    /// position.
    /// </summary>
    /// <remarks>
    /// The value is used as a threshold rather than a validated ratio: at most zero, and <c>NaN</c>, never blends a
    /// position, while at least one always blends it.
    /// </remarks>
    public double Probability { get; init; } = 1;

    public override IntegerVector CrossParents(Parents<IntegerVector> parents, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace) =>
        Cross(random, parents.Parent1, parents.Parent2, searchSpace, Alpha, Probability);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector parent1, IntegerVector parent2, IntegerVectorSearchSpace searchSpace, double alpha, double probability) =>
        Cross(random, parent1, parent2, searchSpace.Minimum, searchSpace.Maximum, alpha, probability);

    public static IntegerVector Cross(IRandomNumberGenerator random, IntegerVector parent1, IntegerVector parent2, IntegerVector minimum, IntegerVector maximum, double alpha, double probability)
    {
        int length = parent1.Count;

        var result = new int[length];

        for (int i = 0; i < length; i++)
        {
            if (random.NextDouble() < probability)
            {
                double value = alpha * parent1[i] + (1.0 - alpha) * parent2[i];
                result[i] = RealVector.RoundToIntegerAt(value, minimum, maximum, i);
            }
            else
            {
                result[i] = parent1[i];
            }
        }

        return IntegerVector.FromOwnedArray(result);
    }
}
