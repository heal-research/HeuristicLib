using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public record BestMedianWorstEntry<TCandidate>(
    EvaluatedCandidate<TCandidate> Best,
    EvaluatedCandidate<TCandidate> Median,
    EvaluatedCandidate<TCandidate> Worst);
