using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class AlgorithmRunAnalysisExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> run)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
    {
        /// <summary>
        /// Attaches a best, median and worst quality analyzer at the end of every iteration the run's algorithm yields.
        /// </summary>
        /// <remarks>
        /// The observation anchor is taken from the run rather than from a separately held algorithm reference, so a
        /// copy produced by <c>with</c> cannot silently go unobserved. Read the result with
        /// <see cref="AlgorithmRun.GetResult{TResult}"/> once the run has started.
        /// </remarks>
        public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> TrackBestMedianWorst(
            out BestMedianWorstAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> analyzer)
        {
            analyzer = Analyzer.BestMedianWorst(run.Algorithm);
            return run.WithAnalyzer(analyzer);
        }
    }
}
