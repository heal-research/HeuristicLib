using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

public static class PopulationQualityTraces
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new ObjectiveVectorsMeasurement<TCandidate>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new CandidatesMeasurement<TCandidate>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }

    extension<TCandidate, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(interceptor, new ObjectiveVectorsMeasurement<TCandidate>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(interceptor, new CandidatesMeasurement<TCandidate>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }
}
