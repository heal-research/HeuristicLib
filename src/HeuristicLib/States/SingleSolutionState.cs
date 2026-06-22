using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record SingleSolutionState<T> : PopulationState<T>
{
    public Solution<T> Solution => Population.Single();
}
