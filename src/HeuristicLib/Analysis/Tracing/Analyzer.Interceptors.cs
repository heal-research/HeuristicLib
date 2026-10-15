using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IInterceptor<TCandidate> interceptor,
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([interceptor], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IReadOnlyList<IInterceptor<TCandidate>> interceptors,
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        TraceAnalyzer<TResult>.Create<IInterceptor<TCandidate>, InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TProblem, TValue>(interceptors,
            static (source, record) => Aspect.Observe(source, record),
            measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IInterceptor<TCandidate> interceptor,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace(interceptor, new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IReadOnlyList<IInterceptor<TCandidate>> interceptors,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace(interceptors, new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IInterceptor<TCandidate> interceptor,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([interceptor], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IReadOnlyList<IInterceptor<TCandidate>> interceptors,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        TraceAnalyzer<TResult>.Create(interceptors,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);

    // Observes at the interface search space and problem, so an implicitly typed lambda needs no type arguments.

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IInterceptor<TCandidate> interceptor,
        Func<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(interceptor, new DelegateMeasurement<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IReadOnlyList<IInterceptor<TCandidate>> interceptors,
        Func<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(interceptors, new DelegateMeasurement<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IInterceptor<TCandidate> interceptor,
        Func<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        Trace([interceptor], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IReadOnlyList<IInterceptor<TCandidate>> interceptors,
        Func<InterceptorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        TraceAnalyzer<TResult>.Create(interceptors,
            static (source, record) => Aspect.Observe(source, record),
            value, clocks, retention);
}
