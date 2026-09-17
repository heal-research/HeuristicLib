using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class PopulationQualityTraces
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new ObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new EvaluatedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(interceptor, new InterceptedObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(interceptor, new InterceptedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }
}
