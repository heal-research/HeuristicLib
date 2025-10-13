using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record ALPSIterationResult<TGenotype> : IIterationResult<TGenotype> {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
