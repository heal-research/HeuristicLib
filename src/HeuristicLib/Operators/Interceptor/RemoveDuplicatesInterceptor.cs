using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public class RemoveDuplicatesInterceptor<TGenotype, TIterationResult>(IEqualityComparer<TGenotype> comparer) : Interceptor<TGenotype, TIterationResult>
  where TIterationResult : IPopulationIterationResult<TGenotype, TIterationResult> {
  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult) {
    var newSolutions = currentIterationResult.Solutions.DistinctBy(s => s.Genotype, comparer);
    return currentIterationResult.WithSolutions(
      new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(newSolutions))
    );
    // return currentIterationResult with {
    //   Solutions = new ImmutableList<Solution<TGenotype>>(newSolutions)
    // };
  }
}
