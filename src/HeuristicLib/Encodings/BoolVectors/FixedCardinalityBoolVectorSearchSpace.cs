using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// The bool vectors of a given length that have exactly <see cref="Cardinality"/> set elements. The expected
/// cardinality is in <c>[0, length]</c>.
/// </summary>
/// <remarks>
/// This is a strict subspace of <see cref="BoolVectorSearchSpace"/> over the same candidate representation, so the two
/// are subsets of one type rather than two types. C# cannot express which operators keep candidates inside such a
/// subset, which is why this space declares its <see cref="Invariants"/> and operators declare matching guarantees.
/// An operator written for the unconstrained space stays valid there and leaves this one, because flipping a single
/// element changes the number of set elements.
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

    /// <remarks>
    /// Built on access rather than cached in a field, because a field would take part in this record's value equality
    /// and make two equal spaces compare unequal. Invariants are read during validation, never during a run.
    /// </remarks>
    public override IReadOnlyList<ISearchInvariant<BoolVector>> Invariants =>
        [new BoolVectorLength(Length), new BoolVectorCardinality(Cardinality)];

    /// <summary>
    /// Widens this space to the unconstrained bool vectors of the same length. Every member of this space is a member
    /// of the result, so a candidate converts in that direction and never back.
    /// </summary>
    public BoolVectorSearchSpace ToUnconstrained() => new(Length);

    internal static int CountSetElements(BoolVector candidate) => BoolVectorCardinality.CountSetElements(candidate);
}
