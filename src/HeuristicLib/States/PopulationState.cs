using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record PopulationState<TCandidate> : SearchState
{
    public required Population<TCandidate> Population { get; init; }
}
