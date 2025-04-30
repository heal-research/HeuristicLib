namespace HEAL.HeuristicLib.Optimization;

public interface ISearchSpace { }

public interface ISearchSpace<in TGenotype> : ISearchSpace {
  bool Contains(TGenotype genotype);
}

public abstract record class SearchSpace<TGenotype> : ISearchSpace<TGenotype> {
  public abstract bool Contains(TGenotype genotype);
}

public interface ISubspaceComparable<in TSearchSpace> where TSearchSpace : ISearchSpace {
  bool IsSubspaceOf(TSearchSpace other);
}

public static class SubspaceComparableExtensions {
  public static bool IsSuperspaceOf<TSearchSpace>(this TSearchSpace searchSpace, TSearchSpace other) where TSearchSpace : ISubspaceComparable<TSearchSpace>, ISearchSpace {
    return other.IsSubspaceOf(searchSpace);
  }
}
