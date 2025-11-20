using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators.Interceptor;

public class RemoveDuplicatesInterceptor<TGenotype, TIterationResult>(IEqualityComparer<TGenotype> comparer) : Interceptor<TGenotype, TIterationResult>
  where TIterationResult : IPopulationIterationResult<TGenotype, TIterationResult> {
  public override TIterationResult Transform(TIterationResult currentIterationResult, TIterationResult? previousIterationResult) {
    var newISolutions = currentIterationResult.Solutions.DistinctBy(s => s.Genotype, comparer);
    return currentIterationResult.WithISolutions(
      new Population<TGenotype>(new ImmutableList<ISolution<TGenotype>>(newISolutions))
    );
    // return currentIterationResult with {
    //   ISolutions = new ImmutableList<Solution<TGenotype>>(newISolutions)
    // };
  }
}
