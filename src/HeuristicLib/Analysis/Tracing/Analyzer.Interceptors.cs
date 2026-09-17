using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([interceptor], measurement, aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors,
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        CreateAggregating<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TProblem, TValue, TResult>(interceptors, measurement, aggregation,
            static (source, record) => new InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(source, record),
            clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace(interceptor, new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace(interceptors, new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure),
            aggregation, clocks, retention, objectiveComparer);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        Trace([interceptor], value, clocks, retention);

    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TResult>(
        IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors,
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        CreateProjected(interceptors, value,
            static (source, record) => new InterceptorObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(source, record),
            clocks, retention);

}
