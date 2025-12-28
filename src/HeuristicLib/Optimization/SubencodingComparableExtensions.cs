using HEAL.HeuristicLib.Encodings;

namespace HEAL.HeuristicLib.Optimization;

public static class SubencodingComparableExtensions {
  public static bool IsSuperspaceOf<TSearchSpace>(this TSearchSpace searchSpace, TSearchSpace other) where TSearchSpace : ISubencodingComparable<TSearchSpace>, IEncoding {
    return other.IsSubspaceOf(searchSpace);
  }
}
