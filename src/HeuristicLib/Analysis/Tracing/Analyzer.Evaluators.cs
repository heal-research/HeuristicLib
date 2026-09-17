using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        CreateAggregating<IEvaluator<TCandidate, TSearchSpace, TProblem>, EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TProblem, TValue, TResult>(evaluators, measurement, aggregation,
            static (source, record) => new EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(source, record),
            clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(evaluator, new DelegateMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        CreateProjected(evaluators, value,
            static (source, record) => new EvaluatorObservationHook<TCandidate, TSearchSpace, TProblem>(source, record),
            clocks, retention);

}
