using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record SingleSolutionState<T> : PopulationState<T>
{
  public ISolution<T> Solution => Population.Single();
}
