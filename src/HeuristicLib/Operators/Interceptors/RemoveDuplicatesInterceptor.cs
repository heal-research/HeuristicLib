using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record RemoveDuplicatesInterceptor<TCandidate, TSearchState>
  : StatelessInterceptor<TCandidate, TSearchState>
  where TSearchState : PopulationState<TCandidate>
{
    public IEqualityComparer<TCandidate> Comparer { get; init; }

    public RemoveDuplicatesInterceptor(IEqualityComparer<TCandidate> comparer)
    {
        Comparer = comparer;
    }

    public override TSearchState Transform(TSearchState currentState, TSearchState? previousState)
      => RemoveDuplicatesInterceptor.Transform(currentState, previousState, Comparer);
}

public static class RemoveDuplicatesInterceptor
{
    public static TSearchState Transform<TCandidate, TSearchState>(
      TSearchState currentState,
      TSearchState? previousState,
      IEqualityComparer<TCandidate> comparer)
      where TSearchState : PopulationState<TCandidate>
    {
        var newSolutions = currentState.Population.DistinctBy(s => s.Candidate, comparer).ToImmutableArray();
        return currentState with
        {
            Population = new Population<TCandidate>(newSolutions)
        };
    }
}
