using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record PopulationSimilarityAnalyzer<TCandidate, TSearchSpace, TProblem, TSearchState>(
    ICandidateSimilarityCalculator<TCandidate> CandidateSimilarity,
    params IInterceptor<TCandidate>[] Interceptor)
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
            observations.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor,
                (populationState, _, _, _, problem) => AfterInterception(result, populationState, problem));
        }
    }

    private void AfterInterception(PopulationSimilarityAnalyzerState bestSolutions, TSearchState currentState,
                                   TProblem problem)
    {
        var candidates = currentState.Population.EvaluatedCandidates
                                     .OrderBy(x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
        var similarities = CandidateSimilarity.CalculateSimilarity(candidates);
        var count = candidates.Length;
        var minSimilarities = new double[count];
        var avgSimilarities = new double[count];
        var maxSimilarities = new double[count];
        for (var i = 0; i < count; i++)
        {
            minSimilarities[i] = 1;
            avgSimilarities[i] = 0;
            maxSimilarities[i] = 0;
            for (var j = 0; j < count; j++)
            {
                if (i == j)
                    continue;

                var similarity = similarities[i, j];

                if (similarity is < 0 or > 1)
                    throw new InvalidOperationException("Solution similarities have to be in the interval [0;1].");

                if (minSimilarities[i] > similarity)
                    minSimilarities[i] = similarity;
                avgSimilarities[i] += similarity;
                if (maxSimilarities[i] < similarity)
                    maxSimilarities[i] = similarity;
            }

            avgSimilarities[i] /= count - 1;
        }

        if (!StoreHistory)
            bestSolutions.Similarities.Clear();
        bestSolutions.Similarities.Add(similarities);
        bestSolutions.AvgSimilarities.Add((minSimilarities.Average(), avgSimilarities.Average(),
            maxSimilarities.Average()));
    }
}

public interface ICandidateSimilarityCalculator<TCandidate>
{
    double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<TCandidate>> candidate);
}

public class PopulationSimilarityAnalyzerState
{
    public List<double[,]> Similarities { get; } = [];
    public List<(double min, double avg, double max)> AvgSimilarities { get; } = [];
}
