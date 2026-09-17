using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class BestSoFarTraces
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        /// <summary>Accumulates the best value across evaluator calls. By default, records only improvements.</summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluator, new ObjectiveVectorsFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>(),
                Aggregate.BestSoFar(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);

        /// <summary>Accumulates the best candidate across evaluator calls. By default, records only improvements.</summary>
        public TraceAnalyzer<EvaluatedCandidate<TCandidate>> TraceBestCandidateSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluator, new EvaluatedCandidatesFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>(),
                Aggregate.BestCandidateSoFar<TCandidate>(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);
    }

    extension<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        /// <summary>Accumulates the best value across several evaluators into one trace.</summary>
        public TraceAnalyzer<ObjectiveVector> TraceBestSoFar(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(evaluators, new ObjectiveVectorsFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>(),
                Aggregate.BestSoFar(), clocks, retention ?? TraceRetention.OnChange(), objectiveComparer);
    }
}
