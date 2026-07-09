using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record AllPopulationsTracker<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm, IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor)
  : Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, List<EvaluatedCandidate<TCandidate>[]>>(Algorithm)
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : PopulationState<TCandidate>
{
    public override List<EvaluatedCandidate<TCandidate>[]> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations, List<EvaluatedCandidate<TCandidate>[]> result)
    {
        observations.Observe(Interceptor, (populationState, _, _, _, _) => AfterInterception(result, populationState));
    }

    public void AfterInterception(List<EvaluatedCandidate<TCandidate>[]> state, PopulationState<TCandidate> populationState) => state.Add(populationState.Population.ToArray());
}
