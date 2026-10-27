using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Moves a randomly chosen inclusive segment to a position in the remaining permutation.</summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record TranslocationMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        Mutate(parent, random);

    public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random)
    {
        if (parent.Count < 2)
            return parent;

        var (start, end) = OrderCrossover.GetRandomBreakPoints(parent.Count, random);
        var insertPoint = random.NextInt(parent.Count - (end - start + 1) + 1);
        return MoveSegment(parent, start, end, insertPoint, invert: false);
    }

    /// <summary>
    /// Moves the inclusive segment to <paramref name="insertPoint"/> in the permutation after removing that segment.
    /// The insertion point may equal the remaining length to append the segment.
    /// </summary>
    public static Permutation Mutate(Permutation parent, int start, int end, int insertPoint) =>
        MoveSegment(parent, start, end, insertPoint, invert: false);

    internal static Permutation MoveSegment(Permutation parent, int start, int end, int insertPoint, bool invert)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(start, parent.Count);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(end, parent.Count);
        var segmentLength = end - start + 1;
        ArgumentOutOfRangeException.ThrowIfNegative(insertPoint);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(insertPoint, parent.Count - segmentLength);

        var result = new int[parent.Count];
        var remainingIndex = 0;
        for (var i = 0; i < result.Length; i++)
        {
            if (i >= insertPoint && i < insertPoint + segmentLength)
            {
                var offset = i - insertPoint;
                result[i] = parent[invert ? end - offset : start + offset];
            }
            else
            {
                result[i] = parent[remainingIndex < start ? remainingIndex : remainingIndex + segmentLength];
                remainingIndex++;
            }
        }

        return Permutation.FromOwnedArray(result);
    }
}
