using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Analyzer<List<BestMedianWorstEntry<TCandidate>>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    /// <summary>
    /// Gets the interceptors observed after each interception.
    /// </summary>
    public ValueArray<IInterceptor<TCandidate>> Interceptors { get; init; }

    /// <summary>
    /// Gets the algorithms observed at the end of every iteration they yield.
    /// </summary>
    public ValueArray<IAlgorithm<TCandidate, TSearchState>> Algorithms { get; init; }

    public BestMedianWorstAnalysis(params IReadOnlyList<IInterceptor<TCandidate>> interceptors)
    {
        Interceptors = interceptors.ToValueArray();
    }

    public override List<BestMedianWorstEntry<TCandidate>> CreateInitialResult() => [];

    public override void RegisterObservations(ObservationPlan observations,
                                              List<BestMedianWorstEntry<TCandidate>> result)
    {
        foreach (var interceptor in Interceptors)
        {
            observations.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor, (populationState, _, _, _, problem) => RecordEntry(result, populationState, problem));
        }

        foreach (var algorithm in Algorithms)
        {
            observations.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, (populationState, _, _, problem) => RecordEntry(result, populationState, problem));
        }
    }

    private static void RecordEntry(List<BestMedianWorstEntry<TCandidate>> bestSolutions, TSearchState currentState, TProblem problem)
    {
        var comparer = problem.Objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(problem.Objective.Directions)
            : problem.Objective.TotalOrderComparer;
        var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comparer).ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("Population is empty, cannot determine best/median/worst solution.");

        bestSolutions.Add(BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
    }
}

public record BestMedianWorstEntry<TCandidate>(EvaluatedCandidate<TCandidate> Best, EvaluatedCandidate<TCandidate> Median, EvaluatedCandidate<TCandidate> Worst);

public static class BestMedianWorstEntry
{
    public static BestMedianWorstEntry<TCandidate> From<TCandidate>(EvaluatedCandidate<TCandidate> best, EvaluatedCandidate<TCandidate> median, EvaluatedCandidate<TCandidate> worst) => new(best, median, worst);
}
