namespace HEAL.HeuristicLib.Optimization;

public interface IEncoding { }

public interface IEncoding<in TGenotype> : IEncoding {
  bool Contains(TGenotype genotype);
}

public abstract record class Encoding<TGenotype> : IEncoding<TGenotype> {
  public abstract bool Contains(TGenotype genotype);
}

public interface ISubencodingComparable<in TSearchSpace> where TSearchSpace : IEncoding {
  bool IsSubspaceOf(TSearchSpace other);
}

public static class SubencodingComparableExtensions {
  public static bool IsSuperspaceOf<TSearchSpace>(this TSearchSpace searchSpace, TSearchSpace other) where TSearchSpace : ISubencodingComparable<TSearchSpace>, IEncoding {
    return other.IsSubspaceOf(searchSpace);
  }
}
