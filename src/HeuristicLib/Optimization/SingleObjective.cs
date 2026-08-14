namespace HEAL.HeuristicLib.Optimization;

public static class SingleObjective
{
    public static readonly ObjectiveDirections Minimize = new([ObjectiveDirection.Minimize], ObjectiveVectorTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Minimize));
    public static readonly ObjectiveDirections Maximize = new([ObjectiveDirection.Maximize], ObjectiveVectorTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Maximize));

    public static ObjectiveDirections Create(ObjectiveDirection direction) => direction switch
    {
        ObjectiveDirection.Minimize => Minimize,
        ObjectiveDirection.Maximize => Maximize,
        _ => throw new InvalidOperationException($"Unsupported objective direction: {direction}.")
    };

    public static ObjectiveDirections WithSingleObjective(this ObjectiveDirections objectives) => Create(objectives.Directions.Single());
}
