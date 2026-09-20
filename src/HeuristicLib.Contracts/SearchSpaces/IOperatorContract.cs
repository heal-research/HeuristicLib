namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// An operator's declared contract in terms of candidate invariants: what it needs of the candidates handed to it,
/// and which invariants hold of the candidates it produces.
/// </summary>
/// <remarks>
/// The two halves decide two separate checks. Accepting more input makes an operator more widely usable, and so does
/// ensuring more about its output, so neither check substitutes for the other.
/// </remarks>
public interface IOperatorContract<TCandidate>
{
    /// <summary>
    /// Determines whether every candidate this operator produces has <paramref name="invariant"/>. Returns
    /// <see langword="null"/> when this operator states nothing about it, which leaves it unchecked.
    /// </summary>
    /// <remarks>
    /// Ordinarily a switch over the invariant, matching by pattern where the operator conforms to a family and by
    /// value where it makes a concrete claim, with a discard arm returning <see langword="null"/>. An answer may
    /// depend on the operator's own configured values, holding over part of a parameter range and not the rest.
    /// </remarks>
    bool? Ensures(ICandidateInvariant<TCandidate> invariant);

    /// <summary>
    /// Gets the invariants a candidate must have for this operator to accept it. Empty means the operator accepts any
    /// candidate of its representation. A requirement is satisfied when the search space states an invariant that
    /// implies it.
    /// </summary>
    IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => [];
}
