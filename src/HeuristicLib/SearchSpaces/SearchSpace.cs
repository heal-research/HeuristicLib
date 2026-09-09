namespace HEAL.HeuristicLib.SearchSpaces;

public abstract record SearchSpace<TCandidate> : ISearchSpace<TCandidate>
{
    public abstract bool Contains(TCandidate candidate);

    /// <summary>
    /// Gets the invariants every member of this space satisfies. The default states none, which leaves compatibility
    /// checking off for the space.
    /// </summary>
    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Invariants => [];
}
