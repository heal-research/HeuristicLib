using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Optimization;

public interface ISubSearchSpaceComparable<in TSearchSpace> where TSearchSpace : ISearchSpace
{
  bool IsSubspaceOf(TSearchSpace other);
}
