namespace HEAL.HeuristicLib.Objectives;

public static class ObjectiveOrderExtensions
{
    /// <summary>Uses an explicit objective comparer, or requires a total order from the objective.</summary>
    public static IComparer<ObjectiveVector> RequireTotalOrder(this ObjectiveDirections objective,
        IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        var totalOrder = objectiveComparer ?? objective.TotalOrderComparer;
        return totalOrder is NoTotalOrderComparer
            ? throw new InvalidOperationException("This operation requires a total order. Supply an objective-vector comparer.")
            : totalOrder;
    }
}
