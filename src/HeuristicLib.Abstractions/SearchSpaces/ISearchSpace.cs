namespace HEAL.HeuristicLib.SearchSpaces;

public interface ISearchSpace;

public interface ISearchSpace<in TGenotype> : ISearchSpace {
    bool Contains(TGenotype genotype);

    //bool IsSubspaceOf(ISearchSpace<TGenotype> other);
}
