using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class ZeroObjective
{
    public static readonly Objective Instance = SingleObjective.Minimize; //TODO should be a zero-objective non-sortable Objective
}
