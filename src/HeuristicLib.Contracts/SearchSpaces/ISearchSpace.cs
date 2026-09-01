namespace HEAL.HeuristicLib.SearchSpaces;

public interface ISearchSpace;

public interface ISearchSpace<TCandidate> : ISearchSpace
{
    bool Contains(TCandidate candidate);

    /// <summary>
    /// Gets the invariants every member of this space satisfies. Empty means the space states no requirement, so every
    /// operator over the candidate representation is compatible with it.
    /// </summary>
    /// <remarks>
    /// These serve both checks. An operator may be used here only if it guarantees each of these of its output, and it
    /// may be used here only if everything it requires of its input is entailed by one of these.
    /// <para>
    /// Declaring invariants is how a space opts in to compatibility checking, and it obliges the operators intended for
    /// it to declare matching guarantees. A space that leaves this empty behaves exactly as it did before invariants
    /// existed.
    /// </para>
    /// </remarks>
    IReadOnlyList<ISearchInvariant<TCandidate>> Invariants => [];

    //bool IsSubspaceOf(ISearchSpace<TCandidate> other);
}
