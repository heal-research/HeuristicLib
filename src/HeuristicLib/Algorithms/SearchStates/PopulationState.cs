namespace HEAL.HeuristicLib.Algorithms;

public record PopulationState<TCandidate> : SearchState
{
    public required Population<TCandidate> Population { get; init; }
}

public static class PopulationState
{
    public static PopulationState<TCandidate> From<TCandidate>(Population<TCandidate> population) => new() { Population = population };
}

public static class PopulationStateExtensions
{
    extension<TCandidate>(Population<TCandidate> population)
    {
        public PopulationState<TCandidate> ToPopulationState() => PopulationState.From(population);
    }
}
