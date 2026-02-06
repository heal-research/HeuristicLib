using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class RemoveDuplicatesInterceptor<TGenotype, TAlgorithmState> 
  : StatelessInterceptor<TGenotype, TAlgorithmState>
  where TGenotype : class
  where TAlgorithmState : PopulationState<TGenotype>
{
  private readonly IEqualityComparer<TGenotype> comparer;
  public RemoveDuplicatesInterceptor(IEqualityComparer<TGenotype> comparer) {
    this.comparer = comparer;
  }

  public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState)
  {
    var newSolutions = currentState.Population.DistinctBy(s => s.Genotype, comparer);
    return currentState with {
      Population = new Population<TGenotype>(newSolutions)
    };
  }
}
