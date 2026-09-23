using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class PopulationQualityTraces
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new ObjectiveVectorsMeasurement<TCandidate, TSearchState>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(algorithm, new EvaluatedCandidatesMeasurement<TCandidate, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }

    /// <remarks>
    /// An interceptor names only its candidate, so the search state it transforms is named on each method.
    /// </remarks>
    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality<TSearchState>(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
            where TSearchState : PopulationState<TCandidate> =>
            Analyzer.Trace(interceptor, new InterceptedObjectiveVectorsMeasurement<TCandidate, TSearchState>(),
                Aggregate.BestMedianWorst(), clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates<TSearchState>(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
            where TSearchState : PopulationState<TCandidate> =>
            Analyzer.Trace(interceptor, new InterceptedCandidatesMeasurement<TCandidate, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(), clocks, retention, objectiveComparer);
    }

    /// <remarks>
    /// An interceptor written on one of the authoring bases names its search state, so these infer it.
    /// </remarks>
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(Interceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            ((IInterceptor<TCandidate>)interceptor).TracePopulationQuality<TCandidate, TSearchState>(clocks, retention, objectiveComparer);

        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            ((IInterceptor<TCandidate>)interceptor).TracePopulationCandidates<TCandidate, TSearchState>(clocks, retention, objectiveComparer);
    }
}
