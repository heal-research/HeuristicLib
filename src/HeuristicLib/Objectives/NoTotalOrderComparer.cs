namespace HEAL.HeuristicLib.Objectives;

public class NoTotalOrderComparer : IComparer<ObjectiveVector>
{
    public static readonly NoTotalOrderComparer Instance = new();
    private NoTotalOrderComparer() { }
    public int Compare(ObjectiveVector? x, ObjectiveVector? y) => throw new InvalidOperationException("No total order defined");
}
