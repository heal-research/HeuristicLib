using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class PopulationTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Traces the whole population of every iteration the algorithm yields.
        /// </summary>
        /// <remarks>This keeps every population, so it is not a default recommendation for long runs.</remarks>
        public static TraceAnalyzer<IReadOnlyList<EvaluatedCandidate<T>>> TraceAllPopulations<T, TR>(
            IAlgorithm<T, TR> algorithm,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null)
            where TR : PopulationState<T> =>
            Analyzer.Trace(
                algorithm,
                new EvaluatedCandidatesMeasurement<T, TR>(),
                Aggregate.Readings<EvaluatedCandidate<T>>(),
                clocks, retention);

        /// <summary>
        /// Traces every objective vector the evaluator produces, one entry per evaluator call.
        /// </summary>
        /// <remarks>
        /// Reading the snapshot gives the full history. Adding an iteration clock and reading by it gives the values of
        /// one iteration, which is what a clearing window used to provide without keeping the rest.
        /// </remarks>
        public static TraceAnalyzer<IReadOnlyList<ObjectiveVector>> TraceAllObjectiveVectors<T>(
            IEvaluator<T> evaluator,
            IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null) =>
            Analyzer.Trace(
                evaluator,
                new ObjectiveVectorsFromEvaluationMeasurement<T>(),
                Aggregate.Readings<ObjectiveVector>(),
                clocks, retention);
    }
}
