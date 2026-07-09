namespace HEAL.HeuristicLib.SearchSpaces;

public interface ISearchSpace;

public interface ISearchSpace<in TCandidate> : ISearchSpace
{
    bool Contains(TCandidate candidate);

    //bool IsSubspaceOf(ISearchSpace<TCandidate> other);
}
