using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Analysis;

public static class Analyzer
{
    /// <summary>
    /// Creates a trace that measures the observations of one anchor, recording every firing.
    /// </summary>
    /// <remarks>
    /// An aggregation or retention that ranks is given the run's objective, which the trace reads from the problem the
    /// observed boundary was called with. Nothing has to name it, and nothing can name the wrong one.
    /// </remarks>
    public static TraceAnalyzer<TResult> Trace<TObservation, TProblem, TValue, TResult>(
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IAnchor<TObservation, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        Trace(measurement, aggregation, Retain.Always<TResult>(), at, clocks);

    /// <summary>
    /// Creates a trace that measures the observations of one anchor. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TObservation, TProblem, TValue, TResult>(
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IAnchor<TObservation, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        new((builder, trace) => builder.Observe(at, observation =>
            trace.Sample(measurement.Read(observation), aggregation, observation.Problem.Objective)), retention, clocks);

    /// <summary>
    /// Creates a trace from a target-typed runtime-only measurement function, recording every firing.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TObservation, TProblem, TValue, TResult>(
        Func<TObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IAnchor<TObservation, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        Trace(new DelegateMeasurement<TObservation, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TObservation, TProblem, TValue, TResult>(
        Func<TObservation, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IAnchor<TObservation, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        Trace(new DelegateMeasurement<TObservation, TValue>(measure), aggregation, retention, at, clocks);
}
