using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record AllPopulationsTracker<T, TS, TP, TR>(IAlgorithm<T, TS, TP, TR> Algorithm, IInterceptor<T, TS, TP, TR> Interceptor)
  : Analyzer<T, TS, TP, TR, List<ISolution<T>[]>>(Algorithm)
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : PopulationState<T>
{
  public override List<ISolution<T>[]> CreateInitialResult() => [];

  public override void RegisterObservations(ObservationPlan observations, List<ISolution<T>[]> result)
  {
    observations.Observe(Interceptor, (populationState, _, _, _, _) => AfterInterception(result, populationState));
  }

  public void AfterInterception(List<ISolution<T>[]> state, PopulationState<T> populationState) => state.Add(populationState.Population.ToArray());
}
