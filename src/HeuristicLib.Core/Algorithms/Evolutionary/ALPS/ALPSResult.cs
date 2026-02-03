using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record AlpsResult<TGenotype> : IAlgorithmState {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
