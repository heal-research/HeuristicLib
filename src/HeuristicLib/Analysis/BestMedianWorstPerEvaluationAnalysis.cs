using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record
    BestMedianWorstPerEvaluationAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> : Analyzer<
    BestMedianWorstPerEvaluationAnalysisState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    private ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }
    private ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> Interceptors { get; }

    public BestMedianWorstPerEvaluationAnalysis(IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators,
                                                IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors)
    {
        Evaluators = evaluators.ToImmutableArray();
        Interceptors = interceptors.ToImmutableArray();
    }

    public override BestMedianWorstPerEvaluationAnalysisState<TCandidate> CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations,
                                              BestMedianWorstPerEvaluationAnalysisState<TCandidate> result)
    {
        foreach (var evaluator in Evaluators)
        {
            observations.Observe(evaluator, (_, candidates, _, _) => result.AfterEvaluation(candidates));
        }

        foreach (var interceptor in Interceptors)
            observations.Observe(interceptor,
                ((populationState, _, _, _, problem) => result.AfterInterception(populationState, problem.Objective)));
    }
}

public sealed class BestMedianWorstPerEvaluationAnalysisState<TCandidate>
{
    private int currentEvaluationsCount;
    private readonly List<(int evaluations, BestMedianWorstEntry<TCandidate> entry)> bestSolutions = [];

    public IReadOnlyList<(int evaluations, BestMedianWorstEntry<TCandidate> entry)> BestSolutions => bestSolutions;

    public void AfterEvaluation(IReadOnlyList<TCandidate> candidates)
    {
        currentEvaluationsCount += candidates.Count;
    }

    public void AfterInterception(PopulationState<TCandidate> currentState, ObjectiveDirections objective)
    {
        if (currentState.Population.EvaluatedCandidates.Count == 0)
        {
            throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");
        }

        var comp = objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(objective.Directions)
            : objective.TotalOrderComparer;
        var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();

        bestSolutions.Add((currentEvaluationsCount,
            BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
    }
}
