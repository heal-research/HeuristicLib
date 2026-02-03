namespace HEAL.HeuristicLib.Optimization;

public interface ISubencodingComparable<in TSearchSpace> where TSearchSpace : IEncoding {
  bool IsSubspaceOf(TSearchSpace other);
}
