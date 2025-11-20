using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record AlpsResult<TGenotype> : IAlgorithmResult {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
