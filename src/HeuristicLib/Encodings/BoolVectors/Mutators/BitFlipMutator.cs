using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// Flips each element independently, which changes the number of set elements and so suits
/// <see cref="BoolVectorSearchSpace"/> rather than <see cref="FixedCardinalityBoolVectorSearchSpace"/>. Use
/// <see cref="BitSwapMutator"/> where the count has to survive.
/// </summary>
public record BitFlipMutator : SingleCandidateMutator<BoolVector, BoolVectorSearchSpace>, IInvariantContract<BoolVector>
{
    /// <summary>
    /// Gets the probability that an element is flipped, or <see langword="null"/> to flip one element per candidate on
    /// average. The expected value is in <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The default scales with the candidate, so a longer vector is not disrupted more than a shorter one. Fixing the
    /// rate instead makes the expected number of flips grow with the length.
    /// </remarks>
    public double? FlipProbability { get; init; }

    /// <remarks>
    /// Length survives a flip and cardinality does not, so this operator is refused over a search space that
    /// constrains the count rather than failing once the run is under way.
    /// </remarks>
    public bool? Ensures(ISearchInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength => true,
        BoolVectorCardinality => false,
        _ => null
    };

    public override BoolVector MutateCandidate(BoolVector parent, IRandomNumberGenerator random, BoolVectorSearchSpace searchSpace) =>
        Mutate(parent, random, FlipProbability);

    /// <summary>
    /// Flips each element with <paramref name="flipProbability"/>, or with <c>1 / length</c> when it is
    /// <see langword="null"/>. An empty candidate is returned unchanged.
    /// </summary>
    public static BoolVector Mutate(BoolVector candidate, IRandomNumberGenerator random, double? flipProbability = null)
    {
        if (candidate.Count == 0)
        {
            return candidate;
        }

        var probability = flipProbability ?? 1.0 / candidate.Count;
        var elements = candidate.ToArray();
        for (var i = 0; i < elements.Length; i++)
        {
            if (random.NextBool(probability))
            {
                elements[i] = !elements[i];
            }
        }

        return BoolVector.FromOwnedArray(elements);
    }
}
