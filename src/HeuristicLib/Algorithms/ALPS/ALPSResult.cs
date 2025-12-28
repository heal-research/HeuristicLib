using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public record AlpsResult<TGenotype> : IIterationResult {
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
