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
    /// An operator may be used here only if it guarantees each of these of its output and everything it requires of
    /// its input is entailed by one of these.
    /// </remarks>
    IReadOnlyList<ISearchInvariant<TCandidate>> Invariants => [];

    //bool IsSubspaceOf(ISearchSpace<TCandidate> other);
}
