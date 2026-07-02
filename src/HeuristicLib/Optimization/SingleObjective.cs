namespace HEAL.HeuristicLib.Optimization;

public static class SingleObjective
{
    public static readonly ObjectiveDirections Minimize = new([ObjectiveDirection.Minimize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Minimize));
    public static readonly ObjectiveDirections Maximize = new([ObjectiveDirection.Maximize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Maximize));

    public static ObjectiveDirections Create(ObjectiveDirection direction) => direction switch
    {
        ObjectiveDirection.Minimize => Minimize,
        ObjectiveDirection.Maximize => Maximize,
        _ => throw new NotImplementedException()
    };

    public static ObjectiveDirections WithSingleObjective(this ObjectiveDirections objectives) => Create(objectives.Directions.Single());
}
