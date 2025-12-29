using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record PopulationIterationState<TGenotype> : IterationState {
  public required Population<TGenotype> Population { get; init; }
}
