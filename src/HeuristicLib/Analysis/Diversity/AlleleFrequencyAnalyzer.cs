using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public record AlleleFrequencyAnalyzer<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlleleCalculator<TCandidate> CandidateSimilarity,
    params IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>[] Interceptor)
    : Analyzer<PopulationSimilarityAnalyzerState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public bool StoreHistory { get; init; } = true;
    public override PopulationSimilarityAnalyzerState CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations,
                                              PopulationSimilarityAnalyzerState result)
    {
        foreach (var interceptor in Interceptor)
        {
            observations.Observe(interceptor,
                (populationState, _, _, _, problem) => AfterInterception(result, populationState, problem));
        }
    }

    private void AfterInterception(PopulationSimilarityAnalyzerState bestSolutions, TSearchState currentState,
                                   TProblem problem)
    {
        var candidates = currentState.Population.EvaluatedCandidates
                                     .OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    }
}

public interface IAlleleCalculator<T>
{
    Allele[] CalculateAlleles(T solution);
}

public record Allele(string Id, double Impact = 0);

public record AlleleFrequency(
    string Id,
    double Frequency,
    double AverageImpact,
    double AverageSolutionQuality,
    bool ContainedInBestKnownSolution,
    bool ContainedInBestSolution);

public class AlleleFrequencyAnalyzerState
{
    private List<List<AlleleFrequency>> History = [];
}
