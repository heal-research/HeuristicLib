using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// The bool vectors of a given length that have exactly <see cref="Cardinality"/> set elements. The expected
/// cardinality is in <c>[0, length]</c>.
/// </summary>
/// <remarks>
/// This is a strict subspace of <see cref="BoolVectorSearchSpace"/> over the same candidate representation, which is
/// what makes the search space type argument carry information the candidate type does not. An operator written for
/// the unconstrained space stays valid there and leaves this one, because flipping a single element changes the
/// number of set elements. Operators intended for this space must therefore preserve cardinality, and they read it
/// from here.
/// <para>
/// A cardinality of zero or of the full length leaves exactly one member, so cardinality preserving operators have no
/// move available and return their input unchanged.
/// </para>
/// </remarks>
public record FixedCardinalityBoolVectorSearchSpace : SearchSpace<BoolVector>
{
    public FixedCardinalityBoolVectorSearchSpace(int length, int cardinality)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(cardinality);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cardinality, length);

        Length = length;
        Cardinality = cardinality;
    }

    public int Length { get; }

    public int Cardinality { get; }

    public override bool Contains(BoolVector candidate) =>
        candidate.Count == Length && CountSetElements(candidate) == Cardinality;

    /// <summary>
    /// Widens this space to the unconstrained bool vectors of the same length. Every member of this space is a member
    /// of the result, so a candidate converts in that direction and never back.
    /// </summary>
    public BoolVectorSearchSpace ToUnconstrained() => new(Length);

    internal static int CountSetElements(BoolVector candidate)
    {
        var count = 0;
        for (var i = 0; i < candidate.Count; i++)
        {
            if (candidate[i])
            {
                count++;
            }
        }

        return count;
    }
}
