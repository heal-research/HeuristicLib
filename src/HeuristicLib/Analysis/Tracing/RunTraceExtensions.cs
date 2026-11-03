using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

public static class RunTraceExtensions
{
    extension<TCandidate, TSearchState>(
        AlgorithmRunSetup<TCandidate, TSearchState> setup)
        where TSearchState : PopulationState<TCandidate>
    {
        /// <summary>Adds a population-candidate trace and returns the setup for further configuration.</summary>
        /// <param name="analyzer">The added analyzer. Read it while the run executes or after it completes.</param>
        /// <param name="algorithm">
        /// The algorithm to observe. When omitted, the trace observes the root algorithm owned by the setup.
        /// </param>
        /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
        /// <param name="retention">
        /// Decides which observations become entries. When omitted, every observation does.
        /// </param>
        /// <param name="objectiveComparer">
        /// Orders objective vectors for ranking. When omitted, the observed problem's objective does.
        /// </param>
        public AlgorithmRunSetup<TCandidate, TSearchState> TracePopulationCandidates(
            out TraceAnalyzer<BestMedianWorstEntry<TCandidate>> analyzer,
            IAlgorithm<TCandidate, TSearchState>? algorithm = null,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null,
            IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            analyzer = (algorithm ?? setup.Algorithm).Iterations.TracePopulationCandidates(clocks, retention, objectiveComparer);
            return setup.AddAspect(analyzer);
        }
    }
}
