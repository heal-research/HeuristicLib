using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record AllObjectiveVectorsAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Analyzer<List<ObjectiveVector>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    private readonly bool resetAfterIntercept;
    private ImmutableArray<IEvaluator<TCandidate>> Evaluators { get; }
    private ImmutableArray<IInterceptor<TCandidate>> Interceptors { get; }

    public AllObjectiveVectorsAnalysis(IReadOnlyList<IEvaluator<TCandidate>> evaluators,
                                       IReadOnlyList<IInterceptor<TCandidate>> interceptors, bool resetAfterIntercept = false)
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
            observations.Observe<TCandidate, TSearchSpace, TProblem>(evaluator,
                (objectiveVectors, _, _, _) => result.AddRange(objectiveVectors));

        if (!resetAfterIntercept)
            return;
        foreach (var interceptor in Interceptors)
            observations.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor,
                (_, _, _, _, _) => result.Clear());
    }
}
