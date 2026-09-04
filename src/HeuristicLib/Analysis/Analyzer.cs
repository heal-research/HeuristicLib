using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class Analyzer
{
    /// <summary>
    /// Creates a trace analyzer that measures a typed algorithm observation.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), Retain.Always<TResult>(), clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        Func<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        Trace(new DelegateMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed crossover observation.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        ICrossover<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), Retain.Always<TResult>(), clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        ICrossover<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed evaluator observation.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IEvaluator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), Retain.Always<TResult>(), clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IEvaluator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed mutator observation.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IMutator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), Retain.Always<TResult>(), clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IMutator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed interceptor observation.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), Retain.Always<TResult>(), clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        Trace(new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure), aggregation, at, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed algorithm observation. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), retention, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed crossover observation. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        ICrossover<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), retention, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed evaluator observation. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IEvaluator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), retention, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed mutator observation. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        IMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IMutator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), retention, clocks);

    /// <summary>
    /// Creates a trace analyzer that measures a typed interceptor observation. Only the firings the retention keeps become entries.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        new((builder, trace) => builder.Observe(at, observation =>
            Sample(observation, observation.Problem.Objective, measurement, aggregation, trace)), retention, clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        Func<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        Trace(new DelegateMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure), aggregation, retention, at, clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        ICrossover<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, retention, at, clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IEvaluator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, retention, at, clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TValue, TResult>(
        Func<MutatorObservation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IMutator<TCandidate, TSearchSpace, TProblem> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        Trace(new DelegateMeasurement<MutatorObservation<TCandidate, TSearchSpace, TProblem>, TValue>(measure), aggregation, retention, at, clocks);

    /// <summary>
    /// Creates a trace analyzer from a target-typed runtime-only measurement function, keeping only the firings the retention keeps.
    /// </summary>
    /// <remarks>An analyzer created with this overload does not have serializable measurement configuration.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchSpace, TProblem, TSearchState, TValue, TResult>(
        Func<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation,
        IRetention<TResult> retention,
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> at,
        params IReadOnlyList<Clock> clocks)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate> =>
        Trace(new DelegateMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, TValue>(measure), aggregation, retention, at, clocks);

    /// <summary>
    /// Turns one observation into one sample: read the values, aggregate them, hand the result to the trace.
    /// </summary>
    private static void Sample<TObservation, TValue, TResult>(
        TObservation observation,
        ObjectiveDirections objective,
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        TraceAnalyzer<TResult> trace)
        where TObservation : Observation
    {
        var readings = measurement.Read(observation);
        var aggregated = aggregation.Aggregate(readings, objective);
        trace.Record(aggregated, objective);
    }
}
