using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public record AlpsAlgorithmState<TGenotype> : IAlgorithmState
{
  public required IReadOnlyList<Population<AgedGenotype<TGenotype>>> Population { get; init; }
}
