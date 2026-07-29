using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstEntry<TCandidate>(
    EvaluatedCandidate<TCandidate> Best,
    EvaluatedCandidate<TCandidate> Median,
    EvaluatedCandidate<TCandidate> Worst);

public static class BestMedianWorstEntry
{
    public static BestMedianWorstEntry<TCandidate> From<TCandidate>(
        EvaluatedCandidate<TCandidate> best, EvaluatedCandidate<TCandidate> median, EvaluatedCandidate<TCandidate> worst) => new(best, median, worst);
}
