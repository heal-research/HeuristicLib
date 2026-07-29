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
    private ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }
    private ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> Interceptors { get; }

    public AllObjectiveVectorsAnalysis(IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators,
                                       IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors, bool resetAfterIntercept = false)
    {
        this.resetAfterIntercept = resetAfterIntercept;
        Evaluators = evaluators.ToImmutableArray();
        Interceptors = interceptors.ToImmutableArray();
    }

    public override List<ObjectiveVector> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations,
                                              List<ObjectiveVector> result)
    {
        foreach (var evaluator in Evaluators)
            observations.Observe(evaluator,
                (_, objectives, _, _) => result.AddRange(objectives));

        if (!resetAfterIntercept)
            return;
        foreach (var interceptor in Interceptors)
            observations.Observe(interceptor,
                (_, _, _, _, _) => result.Clear());
    }
}
