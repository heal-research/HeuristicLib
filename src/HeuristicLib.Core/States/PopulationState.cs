using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record PopulationState<TGenotype> : SearchState
{
  public required Population<TGenotype> Population { get; init; }
}
