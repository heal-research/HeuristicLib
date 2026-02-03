using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public record AlpsResult<TGenotype> : IAlgorithmState
{
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
