using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public interface ICandidateSimilarityCalculator<TCandidate>
{
    double[,] CalculateSimilarity(IReadOnlyList<EvaluatedCandidate<TCandidate>> candidate);
}
