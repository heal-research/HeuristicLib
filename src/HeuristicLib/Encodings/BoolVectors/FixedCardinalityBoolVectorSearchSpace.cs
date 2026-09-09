using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

/// <summary>
/// The bool vectors of a given length that have exactly <see cref="Cardinality"/> set elements. The expected
/// cardinality is in <c>[0, length]</c>.
/// </summary>
/// <remarks>
/// A strict subspace of <see cref="BoolVectorSearchSpace"/> over the same candidate representation. An operator
/// written for the unconstrained space may leave this one, because flipping a single element changes the number of set
/// elements, so this space states its <see cref="Invariants"/> and cardinality preserving operators declare matching
/// guarantees.
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
    /// Invariants are read during validation, never during a run.
    /// </remarks>
    public override IReadOnlyList<ICandidateInvariant<BoolVector>> Invariants =>
        [new BoolVectorLength(Length), new BoolVectorCardinality(Cardinality)];

    /// <summary>
    /// Widens this space to the unconstrained bool vectors of the same length. Every member of this space is a member
    /// of the result, so a candidate converts in that direction and never back.
    /// </summary>
    public BoolVectorSearchSpace ToUnconstrained() => new(Length);

    internal static int CountSetElements(BoolVector candidate) => BoolVectorCardinality.CountSetElements(candidate);
}
