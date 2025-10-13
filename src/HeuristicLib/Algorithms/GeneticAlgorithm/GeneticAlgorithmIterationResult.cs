using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmIterationResult<TGenotype> : IIterationResult<TGenotype> {
  public required Population<TGenotype> Population { get; init; }
}
