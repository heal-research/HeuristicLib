using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record RemoveDuplicatesInterceptor<TGenotype, TAlgorithmState>
  : StatelessInterceptor<TGenotype, TAlgorithmState>
  where TAlgorithmState : PopulationState<TGenotype>
{
  public IEqualityComparer<TGenotype> Comparer { get; init; }

  public RemoveDuplicatesInterceptor(IEqualityComparer<TGenotype> comparer)
  {
    Comparer = comparer;
  }

  public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState)
    => RemoveDuplicatesInterceptor.Transform(currentState, previousState, Comparer);
}

public static class RemoveDuplicatesInterceptor
{
  public static TAlgorithmState Transform<TGenotype, TAlgorithmState>(
    TAlgorithmState currentState,
    TAlgorithmState? previousState,
    IEqualityComparer<TGenotype> comparer)
    where TAlgorithmState : PopulationState<TGenotype>
  {
    var newSolutions = currentState.Population.DistinctBy(s => s.Genotype, comparer).ToImmutableArray();
    return currentState with {
      Population = new Population<TGenotype>(newSolutions)
    };
  }
}
