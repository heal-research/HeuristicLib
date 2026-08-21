namespace HEAL.HeuristicLib.SearchSpaces;

public abstract record SearchSpace<TCandidate> : ISearchSpace<TCandidate>
{
    public abstract bool Contains(TCandidate candidate);
}
