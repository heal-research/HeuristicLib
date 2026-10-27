using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Moves and reverses a randomly chosen inclusive segment.</summary>
/// <remarks>Permutations with fewer than two elements are returned unchanged without random draws.</remarks>
public sealed record TranslocationInversionMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        Mutate(parent, random);

    public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random)
    {
        if (parent.Count < 2)
            return parent;

        var (start, end) = OrderCrossover.GetRandomBreakPoints(parent.Count, random);
        var insertPoint = random.NextInt(parent.Count - (end - start + 1) + 1);
        return TranslocationMutator.MoveSegment(parent, start, end, insertPoint, invert: true);
    }

    /// <summary>
    /// Moves and reverses the inclusive segment, using an insertion point in the remaining permutation.
    /// The insertion point may equal the remaining length to append the segment.
    /// </summary>
    public static Permutation Mutate(Permutation parent, int start, int end, int insertPoint) =>
        TranslocationMutator.MoveSegment(parent, start, end, insertPoint, invert: true);
}
