using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record PopulationIterationResult<TGenotype> : IIterationResult<TGenotype> {
  public required Population<TGenotype> Population { get; init; }
}
