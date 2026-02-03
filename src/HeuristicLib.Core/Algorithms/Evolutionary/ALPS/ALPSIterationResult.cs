using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record AlpsAlgorithmState<TGenotype> : IAlgorithmState {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
