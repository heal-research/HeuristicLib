namespace HEAL.HeuristicLib.Optimization;

public static class ZeroObjective
{
    public static readonly ObjectiveDirections Instance = SingleObjective.Minimize; //TODO should be a zero-objective non-sortable Objective
}
