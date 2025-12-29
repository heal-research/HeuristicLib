using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record PopulationIterationState<TGenotype> : AlgorithmState {
  public required Population<TGenotype> Population { get; init; }
}
