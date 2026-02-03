using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Optimization;

public abstract record SearchSpace<TGenotype> : ISearchSpace<TGenotype>
{
  public abstract bool Contains(TGenotype genotype);

  //public abstract bool IsSubspaceOf(IEncoding<TGenotype> other);
}
