namespace HEAL.HeuristicLib.Optimization;

public class NoTotalOrderComparer : IComparer<ObjectiveVector>
{
  public int Compare(ObjectiveVector? x, ObjectiveVector? y) => throw new InvalidOperationException("No total order defined");
}
