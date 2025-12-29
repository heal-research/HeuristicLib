using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public class RemoveDuplicatesInterceptor<TGenotype, TIterationState>(IEqualityComparer<TGenotype> comparer) : Interceptor<TGenotype, TIterationState>
  where TIterationState : PopulationIterationState<TGenotype> {
  public override TIterationState Transform(TIterationState currenTIterationState, TIterationState? previousIterationState) {
    var newSolutions = currenTIterationState.Population.DistinctBy(s => s.Genotype, comparer);
    return currenTIterationState with {
      Population = new Population<TGenotype>(newSolutions)
    };
  }
}
