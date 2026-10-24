using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The pairwise similarity of one population, and the smallest, mean and largest average similarity over it.
/// </summary>
/// <remarks>
/// <see cref="Matrix"/> is the raw similarity of every candidate pair, indexed in the order the candidates were ranked
/// by the run's objective. Rows are immutable snapshots of the calculator output.
/// </remarks>
public sealed record PopulationSimilarity(ValueArray<ValueArray<double>> Matrix, MinMeanMax Average);

public interface ICandidateSimilarityCalculator<TCandidate>
{
    double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<TCandidate>> candidate);
}

public static class PopulationSimilarityExtensions
{
    extension<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population)
    {
        /// <summary>Calculates the pairwise similarity of the population and the summary over it.</summary>
        /// <remarks>The matrix is indexed in the order the candidates rank by the objective directions.</remarks>
        public PopulationSimilarity Similarity(ICandidateSimilarityCalculator<TCandidate> candidateSimilarity,
            ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            if (population.Count == 0)
                throw new InvalidOperationException("There are no candidates, cannot determine population similarity.");

            var candidates = population.OrderBy(candidate => candidate.ObjectiveVector, objective.RequireTotalOrder(objectiveComparer)).ToArray();
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
                Enumerable.Range(0, count).Select(row =>
                    Enumerable.Range(0, count).Select(column => similarities[row, column]).ToValueArray()).ToValueArray(),
                new MinMeanMax(minSimilarities.Average(), meanSimilarities.Average(), maxSimilarities.Average()));
        }
    }
}

public static class PopulationSimilarityTraces
{
    extension<TCandidate, TSearchState>(ObservationSource<AlgorithmObservation<TCandidate, TSearchState>> iterations)
        where TSearchState : PopulationState<TCandidate>
    {
        /// <summary>
        /// Traces the pairwise similarity of the population each iteration yields, matrix included.
        /// </summary>
        public TraceAnalyzer<PopulationSimilarity> TracePopulationSimilarity(
            ICandidateSimilarityCalculator<TCandidate> candidateSimilarity,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null) =>
            Analyzer.Trace(iterations,
                observation => observation.State.Population.EvaluatedCandidates.Similarity(candidateSimilarity, observation.Problem.Objective),
                clocks, retention);

        /// <summary>
        /// Traces the smallest, mean and largest average pairwise similarity of the population each iteration yields,
        /// without keeping the matrix it came from.
        /// </summary>
        /// <remarks>
        /// Prefer this over <c>TracePopulationSimilarity</c> when only the summary is read: a trace keeps every entry,
        /// and a matrix per iteration grows with the square of the population size.
        /// </remarks>
        public TraceAnalyzer<MinMeanMax> TraceAverageSimilarity(
            ICandidateSimilarityCalculator<TCandidate> candidateSimilarity,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null) =>
            Analyzer.Trace(iterations,
                observation => observation.State.Population.EvaluatedCandidates.Similarity(candidateSimilarity, observation.Problem.Objective).Average,
                clocks, retention);
    }
}
