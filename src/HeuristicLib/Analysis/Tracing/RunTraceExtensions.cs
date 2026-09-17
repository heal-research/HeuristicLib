using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class RunTraceExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> run)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        /// <summary>Attaches a population-candidate trace and returns the run for further configuration.</summary>
        /// <param name="analyzer">The attached analyzer. Read it while the run executes or after it completes.</param>
        /// <param name="algorithm">
        /// The algorithm to observe. When omitted, the trace observes the root algorithm owned by the run.
        /// </param>
        public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> TracePopulationCandidates(
            out TraceAnalyzer<BestMedianWorstEntry<TCandidate>> analyzer,
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>? algorithm = null,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null,
            IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            analyzer = (algorithm ?? run.Algorithm).TracePopulationCandidates(clocks, retention, objectiveComparer);
            return run.AddAnalyzer(analyzer);
        }
    }
}
