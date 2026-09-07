using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The pairwise similarity of one population, and the smallest, mean and largest average similarity over it.
/// </summary>
/// <remarks>
/// <see cref="Matrix"/> is the raw similarity of every candidate pair, indexed in the order the candidates were ranked
/// by the run's objective. It is kept as written rather than copied, so treat it as read-only.
/// </remarks>
public sealed record PopulationSimilarity(double[,] Matrix, MinMeanMax Average);

public interface ICandidateSimilarityCalculator<TCandidate>
{
    double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<TCandidate>> candidate);
}

/// <summary>
/// Reduces a population to its pairwise similarity matrix and the summary over it.
/// </summary>
public sealed class PopulationSimilarityAggregation<TCandidate>(ICandidateSimilarityCalculator<TCandidate> candidateSimilarity)
    : IAggregation<EvaluatedCandidate<TCandidate>, PopulationSimilarity>
{
    public PopulationSimilarity Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine population similarity.");

        var candidates = readings.OrderBy(candidate => candidate.ObjectiveVector, objective.TotalOrderComparer).ToArray();
        var similarities = candidateSimilarity.CalculateSimilarity(candidates);
        var count = candidates.Length;
        var minSimilarities = new double[count];
        var meanSimilarities = new double[count];
        var maxSimilarities = new double[count];

        for (var i = 0; i < count; i++)
        {
            minSimilarities[i] = 1;
            for (var j = 0; j < count; j++)
            {
                if (i == j)
                    continue;

                var similarity = similarities[i, j];
                if (similarity is < 0 or > 1)
                    throw new InvalidOperationException("Solution similarities have to be in the interval [0;1].");

                if (minSimilarities[i] > similarity)
                    minSimilarities[i] = similarity;
                meanSimilarities[i] += similarity;
                if (maxSimilarities[i] < similarity)
                    maxSimilarities[i] = similarity;
            }

            meanSimilarities[i] /= count - 1;
        }

        return new PopulationSimilarity(
            similarities,
            new MinMeanMax(minSimilarities.Average(), meanSimilarities.Average(), maxSimilarities.Average()));
    }
}

/// <summary>
/// Reduces a population to the summary of its pairwise similarity, without keeping the matrix it came from.
/// </summary>
/// <remarks>
/// Prefer this over <see cref="PopulationSimilarityAggregation{TCandidate}"/> when only the summary is read: a trace
/// keeps every entry, and a matrix per iteration grows with the square of the population size.
/// </remarks>
public sealed class AverageSimilarityAggregation<TCandidate>(ICandidateSimilarityCalculator<TCandidate> candidateSimilarity)
    : IAggregation<EvaluatedCandidate<TCandidate>, MinMeanMax>
{
    private readonly PopulationSimilarityAggregation<TCandidate> full = new(candidateSimilarity);

    public MinMeanMax Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective) =>
        full.Aggregate(readings, objective).Average;
}

public static class PopulationSimilarityTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Traces the pairwise similarity of every population the algorithm yields, matrix included.
        /// </summary>
        public static TraceAnalyzer<PopulationSimilarity> TracePopulationSimilarity<T, TS, TP, TR>(
            ICandidateSimilarityCalculator<T> candidateSimilarity,
            IAlgorithm<T, TS, TP, TR> algorithm,
            params IReadOnlyList<Clock> clocks)
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS>
            where TR : PopulationState<T> =>
            Analyzer.Trace(
                new EvaluatedCandidatesMeasurement<T, TS, TP, TR>(),
                new PopulationSimilarityAggregation<T>(candidateSimilarity),
                Anchor.At(algorithm),
                clocks);

        /// <summary>
        /// Traces the smallest, mean and largest average pairwise similarity of every population the algorithm yields.
        /// </summary>
        public static TraceAnalyzer<MinMeanMax> TraceAverageSimilarity<T, TS, TP, TR>(
            ICandidateSimilarityCalculator<T> candidateSimilarity,
            IAlgorithm<T, TS, TP, TR> algorithm,
            params IReadOnlyList<Clock> clocks)
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS>
            where TR : PopulationState<T> =>
            Analyzer.Trace(
                new EvaluatedCandidatesMeasurement<T, TS, TP, TR>(),
                new AverageSimilarityAggregation<T>(candidateSimilarity),
                Anchor.At(algorithm),
                clocks);
    }
}
