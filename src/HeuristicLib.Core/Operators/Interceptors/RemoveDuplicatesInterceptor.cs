using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class RemoveDuplicatesInterceptor<TGenotype, TAlgorithmState>(IEqualityComparer<TGenotype> comparer) : Interceptor<TGenotype, TAlgorithmState>
  where TAlgorithmState : PopulationState<TGenotype>
{
  public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState)
  {
    var newSolutions = currentState.Population.DistinctBy(s => s.Genotype, comparer);
    return currentState with {
      Population = new Population<TGenotype>(newSolutions)
    };
  }
}
