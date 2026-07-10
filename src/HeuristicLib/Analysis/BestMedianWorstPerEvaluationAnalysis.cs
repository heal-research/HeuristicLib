using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstPerEvaluationAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> : Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, BestMedianWorstPerEvaluationAnalysisState<TCandidate>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : PopulationState<TCandidate>
{
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators { get; }
    private IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[] Interceptors { get; }

    public BestMedianWorstPerEvaluationAnalysis(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm,
                                                IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluators,
                                                IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[] Interceptors) : base(Algorithm)
    {
        this.Evaluators = Evaluators;
        this.Interceptors = Interceptors;
    }

    public override BestMedianWorstPerEvaluationAnalysisState<TCandidate> CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, BestMedianWorstPerEvaluationAnalysisState<TCandidate> result)
    {
        foreach (var evaluator in Evaluators)
        {
            observations.Observe(evaluator, (candidates, _, _, _) => result.AfterEvaluation(candidates));
        }

        foreach (var interceptor in Interceptors)
            observations.Observe(interceptor, ((populationState, _, _, _, problem) => result.AfterInterception(populationState, problem.Objective)));
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
        if (currentState.Population.EvaluatedCandidates.Length == 0)
        {
            throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");
        }

        var comp = objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(objective.Directions) : objective.TotalOrderComparer;
        var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();

        bestSolutions.Add((currentEvaluationsCount, new BestMedianWorstEntry<TCandidate>(ordered[0], ordered[ordered.Length / 2], ordered[^1])));
    }
}
