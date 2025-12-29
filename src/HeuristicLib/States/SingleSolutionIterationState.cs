using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record SingleSolutionIterationState<T> : PopulationIterationState<T> {
  public ISolution<T> Solution => Population.Single();
}
