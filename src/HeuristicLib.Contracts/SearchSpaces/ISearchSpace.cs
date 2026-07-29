namespace HEAL.HeuristicLib.SearchSpaces;

public interface ISearchSpace;

public interface ISearchSpace<TCandidate> : ISearchSpace
{
    bool Contains(TCandidate candidate);

    //bool IsSubspaceOf(ISearchSpace<TCandidate> other);
}
