using HEAL.HeuristicLib.Objectives;
using TreesearchLib;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public readonly struct ObjectiveVectorQuality(ObjectiveVector vector, ObjectiveDirections objective) : IQuality<ObjectiveVectorQuality>
{
    private readonly ObjectiveVector vector = vector;

    public int CompareTo(ObjectiveVectorQuality other) => objective.TotalOrderComparer.Compare(vector, other.vector);

    public bool IsBetter(ObjectiveVectorQuality other) => CompareTo(other) < 0;
}
