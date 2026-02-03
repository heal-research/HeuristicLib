using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Optimization;

public static class SubencodingComparableExtensions
{
  public static bool IsSuperspaceOf<TSearchSpace>(this TSearchSpace searchSpace, TSearchSpace other) where TSearchSpace : ISubencodingComparable<TSearchSpace>, ISearchSpace => other.IsSubspaceOf(searchSpace);
}
