using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record AllPopulationsAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Interceptor)
    : Analyzer<List<EvaluatedCandidate<TCandidate>[]>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public override List<EvaluatedCandidate<TCandidate>[]> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations,
                                              List<EvaluatedCandidate<TCandidate>[]> result)
    {
        observations.Observe(Interceptor, (populationState, _, _, _, _) => AfterInterception(result, populationState));
    }

    public void AfterInterception(List<EvaluatedCandidate<TCandidate>[]> state,
                                  PopulationState<TCandidate> populationState) =>
        state.Add(populationState.Population.ToArray());
}
