namespace HEAL.HeuristicLib.SearchSpaces;

public abstract record SearchSpace<TCandidate> : ISearchSpace<TCandidate>
{
    public abstract bool Contains(TCandidate candidate);

    /// <summary>
    /// Gets the invariants every member of this space satisfies. The default states none, so no operator is checked
    /// for what its output ensures, while an operator that requires an invariant of its input is still rejected.
    /// </summary>
    public virtual ValueArray<ICandidateInvariant<TCandidate>> Invariants => [];
}
