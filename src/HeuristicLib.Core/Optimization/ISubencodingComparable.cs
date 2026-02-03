using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Optimization;

public interface ISubencodingComparable<in TSearchSpace> where TSearchSpace : ISearchSpace
{
  bool IsSubspaceOf(TSearchSpace other);
}
