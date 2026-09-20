using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class BestSoFarTraces
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        /// <summary>Accumulates the best value across evaluator calls. By default, records only improvements.</summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluator, new ObjectiveVectorsFromEvaluationMeasurement<TCandidate>(),
                Aggregate.BestSoFar(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);

        /// <summary>Accumulates the best candidate across evaluator calls. By default, records only improvements.</summary>
        public TraceAnalyzer<EvaluatedCandidate<TCandidate>> TraceBestCandidateSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluator, new EvaluatedCandidatesFromEvaluationMeasurement<TCandidate>(),
                Aggregate.BestCandidateSoFar<TCandidate>(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);
    }

    extension<TCandidate>(IReadOnlyList<IEvaluator<TCandidate>> evaluators)
    {
        /// <summary>Accumulates the best value across several evaluators into one trace.</summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluators, new ObjectiveVectorsFromEvaluationMeasurement<TCandidate>(),
                Aggregate.BestSoFar(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);
    }
}
