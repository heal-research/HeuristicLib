namespace HEAL.HeuristicLib;

public interface ISearchSpace { }

public interface ISearchSpace<in TGenotype> : ISearchSpace {
  bool Contains(TGenotype genotype);
  //bool IsValidOperator(ISearchSpaceOperator<TGenotype, TSelf> @operator);
}

public abstract record class SearchSpace<TGenotype> : ISearchSpace<TGenotype> {
  protected SearchSpace() { }
  public abstract bool Contains(TGenotype genotype);
  // public virtual bool IsValidOperator(ISearchSpaceOperator<TGenotype, TSelf> @operator) {
  //   return @operator.SearchSpace.Equals(this);
  // }
}

public interface ISubspaceComparable<in TSearchSpace> where TSearchSpace : ISearchSpace {
  bool IsSubspaceOf(TSearchSpace other);
}

public static class SubspaceComparableExtensions {
  public static bool IsSuperspaceOf<TSearchSpace>(this TSearchSpace searchSpace, TSearchSpace other) where TSearchSpace : ISubspaceComparable<TSearchSpace>, ISearchSpace {
    return other.IsSubspaceOf(searchSpace);
  }
}
