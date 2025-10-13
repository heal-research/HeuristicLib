using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmResult<TGenotype> : IAlgorithmResult<TGenotype> {
  public required Population<TGenotype> Population { get; init; }
}
