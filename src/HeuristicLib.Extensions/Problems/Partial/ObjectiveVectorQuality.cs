using HEAL.HeuristicLib.Optimization;
using TreesearchLib;

namespace HEAL.HeuristicLib.Problems.Partial;

public readonly struct ObjectiveVectorQuality(ObjectiveVector vector, Objective objective) : IQuality<ObjectiveVectorQuality>
{
  private readonly ObjectiveVector vector = vector;
  public int CompareTo(ObjectiveVectorQuality other) => objective.TotalOrderComparer.Compare(vector, other.vector);

  public bool IsBetter(ObjectiveVectorQuality other) => CompareTo(other) < 0;
}