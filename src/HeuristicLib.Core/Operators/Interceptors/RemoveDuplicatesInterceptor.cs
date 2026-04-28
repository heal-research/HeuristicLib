using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record RemoveDuplicatesInterceptor<TGenotype, TSearchState>
  : StatelessInterceptor<TGenotype, TSearchState>
  where TSearchState : PopulationState<TGenotype>
{
  public IEqualityComparer<TGenotype> Comparer { get; init; }

  public RemoveDuplicatesInterceptor(IEqualityComparer<TGenotype> comparer)
  {
    Comparer = comparer;
  }

  public override TSearchState Transform(TSearchState currentState, TSearchState? previousState)
    => RemoveDuplicatesInterceptor.Transform(currentState, previousState, Comparer);
}

public static class RemoveDuplicatesInterceptor
{
  public static TSearchState Transform<TGenotype, TSearchState>(
    TSearchState currentState,
    TSearchState? previousState,
    IEqualityComparer<TGenotype> comparer)
    where TSearchState : PopulationState<TGenotype>
  {
    var newSolutions = currentState.Population.DistinctBy(s => s.Genotype, comparer).ToImmutableArray();
    return currentState with {
      Population = new Population<TGenotype>(newSolutions)
    };
  }
}
