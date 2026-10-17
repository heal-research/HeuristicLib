using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IOperator<Evaluation, TCandidate> evaluator,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators,
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        TraceAnalyzer<TResult>.Create<IOperator<Evaluation, TCandidate>, EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TProblem, TValue>(evaluators,
            static (source, record) => Aspect.Observe(source, record),
            measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IOperator<Evaluation, TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(evaluator, new DelegateMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IOperator<Evaluation, TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace([evaluator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TResult>(
        IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators,
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        TraceAnalyzer<TResult>.Create(evaluators,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);

    // Observes at the interface search space and problem, so an implicitly typed lambda needs no type arguments.

    public static TraceAnalyzer<TResult> Trace<TCandidate, TValue, TResult>(
        IOperator<Evaluation, TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        Trace(evaluator, new DelegateMeasurement<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IOperator<Evaluation, TCandidate> evaluator,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        Trace([evaluator], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TResult>(
        IReadOnlyList<IOperator<Evaluation, TCandidate>> evaluators,
        Func<EvaluatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        TraceAnalyzer<TResult>.Create(evaluators,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);
}
