using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The ready-made trace of the best, median and worst evaluated candidate of an observed population.
/// </summary>
public static class BestMedianWorstTrace
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a trace of the best, median and worst evaluated candidate of every population the algorithm yields.
        /// </summary>
        public static TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TraceBestMedianWorst<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> at,
            params IReadOnlyList<Clock> clocks)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : PopulationState<TCandidate> =>
            Analyzer.Trace(
                new EvaluatedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(),
                at,
                clocks);

        /// <summary>
        /// Creates a trace of the best, median and worst evaluated candidate of every population an interceptor produces.
        /// </summary>
        public static TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TraceBestMedianWorst<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> at,
            params IReadOnlyList<Clock> clocks)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : PopulationState<TCandidate> =>
            Analyzer.Trace(
                new InterceptedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>(),
                Aggregate.BestMedianWorst<TCandidate>(),
                at,
                clocks);
    }
}
