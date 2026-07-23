using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record AllObjectiveVectorsAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Analyzer<List<ObjectiveVector>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    private readonly bool resetAfterIntercept;
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators { get; }
    private IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[] Interceptors { get; }

    public AllObjectiveVectorsAnalysis(IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators,
                                       IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[]
                                           Interceptors, bool resetAfterIntercept = false)
    {
        this.resetAfterIntercept = resetAfterIntercept;
        this.Evaluators = Evaluators;
        this.Interceptors = Interceptors;
    }

    public override List<ObjectiveVector> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations,
                                              List<ObjectiveVector> result)
    {
        foreach (var evaluator in Evaluators)
            observations.Observe(evaluator,
                (_, evaluatedCandidates, _, _) =>
                    result.AddRange(evaluatedCandidates.Select(candidate => candidate.ObjectiveVector)));

        if (!resetAfterIntercept)
            return;
        foreach (var interceptor in Interceptors)
            observations.Observe(interceptor,
                (_, _, _, _, _) => result.Clear());
    }
}
