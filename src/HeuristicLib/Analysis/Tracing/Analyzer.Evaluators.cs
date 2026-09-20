using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IEvaluator<TCandidate> evaluator,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IReadOnlyList<IEvaluator<TCandidate>> evaluators,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        CreateAggregating<IEvaluator<TCandidate>, EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TProblem, TValue, TResult>(evaluators, measurement, aggregation,
            static (source, record) => new EvaluatorObservationModule<TCandidate, TSearchSpace, TProblem>(source, record),
            clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IEvaluator<TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(evaluator, new DelegateMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IEvaluator<TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IReadOnlyList<IEvaluator<TCandidate>> evaluators,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        CreateProjected(evaluators, value,
            static (source, record) => new EvaluatorObservationModule<TCandidate, TSearchSpace, TProblem>(source, record),
            clocks, retention);

    // Observes at the interface search space and problem, so an implicitly typed lambda needs no type arguments.

    public static TraceAnalyzer<TResult> Trace<TCandidate, TValue, TResult>(
        IEvaluator<TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        Trace(evaluator, new DelegateMeasurement<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IEvaluator<TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        Trace([evaluator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IReadOnlyList<IEvaluator<TCandidate>> evaluators,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        CreateProjected(evaluators, value,
            static (source, record) => new EvaluatorObservationModule<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(source, record),
            clocks, retention);
}
