using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// Clears one randomly chosen set element and sets one randomly chosen cleared element, so the number of set elements
/// is unchanged. This keeps a candidate inside <see cref="FixedCardinalityBoolVectorSearchSpace"/>, which a single
/// element flip would not.
/// </summary>
/// <remarks>
/// The target cardinality is read from the search space, not inferred from the candidate, so a candidate that arrived
/// off cardinality is moved toward the space instead of having its error preserved.
/// </remarks>
public record BitSwapMutator
    : SingleCandidateMutator<BoolVector, FixedCardinalityBoolVectorSearchSpace>, IOperatorContract<BoolVector>
{
    /// <summary>
    /// Length and cardinality both survive a swap, and no input invariant is needed, so this operator is usable over
    /// constrained and unconstrained bool vector spaces alike.
    /// </summary>
    public bool? Ensures(ICandidateInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength or BoolVectorCardinality => true,
        _ => null
    };

    public override BoolVector MutateCandidate(
        BoolVector parent,
        IRandomNumberGenerator random,
        FixedCardinalityBoolVectorSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace);

    /// <summary>
    /// Swaps one set and one cleared element, moving the result to the search space's cardinality when the input does
    /// not already have it.
    /// </summary>
    public static BoolVector Mutate(
        BoolVector candidate,
        IRandomNumberGenerator random,
        FixedCardinalityBoolVectorSearchSpace searchSpace) =>
        Mutate(candidate, random, searchSpace.Cardinality);

    /// <summary>
    /// Swaps one set and one cleared element, moving the result to <paramref name="cardinality"/> when the input does
    /// not already have it. The candidate is returned unchanged when no swap can change it.
    /// </summary>
    /// <remarks>
    /// A candidate whose set count is already <paramref name="cardinality"/> keeps it. A candidate above the target
    /// loses one set element, and one below gains one, so repeated application converges to the target.
    /// </remarks>
    public static BoolVector Mutate(BoolVector candidate, IRandomNumberGenerator random, int cardinality)
    {
        var setCount = FixedCardinalityBoolVectorSearchSpace.CountSetElements(candidate);
        var clearedCount = candidate.Count - setCount;

        // At the target both halves of the swap happen, so both an element to clear and one to set must exist.
        // Away from it only the half that closes the gap happens, and the element it needs exists by that gap.
        var clearOne = setCount >= cardinality;
        var setOne = setCount <= cardinality;
        if ((clearOne && setCount == 0) || (setOne && clearedCount == 0))
        {
            return candidate;
        }

        var elements = candidate.ToArray();

        if (clearOne)
        {
            elements[FindNthMatch(candidate, value: true, random.NextInt(setCount))] = false;
        }

        if (setOne)
        {
            elements[FindNthMatch(candidate, value: false, random.NextInt(clearedCount))] = true;
        }

        return BoolVector.FromOwnedArray(elements);
    }

    private static int FindNthMatch(BoolVector candidate, bool value, int n)
    {
        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i] != value)
            {
                continue;
            }

            if (n == 0)
            {
                return i;
            }

            n--;
        }

        throw new ArgumentOutOfRangeException(nameof(n), n, "Fewer matching elements than the requested position.");
    }
}
