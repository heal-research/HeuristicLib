using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(
    params IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[] Interceptor)
    : Analyzer<List<BestMedianWorstEntry<TCandidate>>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public override List<BestMedianWorstEntry<TCandidate>> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations,
                                              List<BestMedianWorstEntry<TCandidate>> result)
    {
        foreach (var interceptor in Interceptor)
        {
            observations.Observe(interceptor,
                (populationState, _, _, _, problem) => AfterInterception(result, populationState, problem));
        }
    }

    private static void AfterInterception(List<BestMedianWorstEntry<TCandidate>> bestSolutions,
                                          TSearchState currentState, TProblem problem)
    {
        var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(problem.Objective.Directions)
            : problem.Objective.TotalOrderComparer;
        var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
        if (ordered.Length == 0)
        {
            bestSolutions.Add(null!);
            return;
        }

        bestSolutions.Add(BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
    }
}
