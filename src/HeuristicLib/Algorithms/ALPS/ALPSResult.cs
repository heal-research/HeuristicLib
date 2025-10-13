using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record ALPSResult<TGenotype> : IAlgorithmResult<TGenotype> {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
