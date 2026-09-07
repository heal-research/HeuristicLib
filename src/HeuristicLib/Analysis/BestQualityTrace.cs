using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The ready-made trace of the best evaluated candidate an evaluator has produced.
/// </summary>
public static class BestQualityTrace
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a trace of the best evaluated candidate an evaluator has produced, recording only when it improves.
        /// </summary>
        /// <remarks>
        /// One entry is recorded per evaluator call rather than per candidate, because a batch may be evaluated in parallel
        /// and has no inherent order within it. Pair it with <see cref="Clock.FromEvaluations"/> to read the curve against
        /// the number of candidates evaluated.
        /// </remarks>
        public static TraceAnalyzer<EvaluatedCandidate<TCandidate>> TraceBestQuality<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> at,
            params IReadOnlyList<Clock> clocks)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            Analyzer.Trace(
                new EvaluatedCandidatesFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>(),
                Aggregate.Best<TCandidate>(),
                Retain.OnImprovement<TCandidate>(),
                Anchor.At(at),
                clocks);
    }
}
