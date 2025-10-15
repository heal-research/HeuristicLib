using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

public record PopulationResult<TGenotype> : IAlgorithmResult<TGenotype> {
  public required Population<TGenotype> Population { get; init; }
}
