using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Builds traces over the observations of algorithms and operators.</summary>
/// <remarks>
/// A trace observes the operators or algorithms it is given, such as <c>Analyzer.Trace(crossover, o =&gt; o.Offspring.Count)</c>.
/// A trace over the same source given twice observes it once, because sources are matched by reference.
/// </remarks>
public static class Analyzer
{
    /// <summary>Traces one value per call of the operator.</summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TResult>(
        IOperator<TOperation, TCandidate> source,
        Func<OperatorObservation<TOperation, TCandidate>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        Trace([source], value, clocks, retention);

    /// <summary>Traces one value per call of any of the operators.</summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TResult>(
        IReadOnlyList<IOperator<TOperation, TCandidate>> sources,
        Func<OperatorObservation<TOperation, TCandidate>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        TraceAnalyzer<TResult>.Create(sources, static (source, record) => Aspect.Observe(source, record), value, clocks, retention);

    /// <summary>Traces the aggregate of the values measured in each call of the operator, using the observed run's objective.</summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TValue, TResult>(
        IOperator<TOperation, TCandidate> source,
        IMeasurement<OperatorObservation<TOperation, TCandidate>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        Trace([source], measurement, aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each call of any of the operators, using the observed run's
    /// objective.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TValue, TResult>(
        IReadOnlyList<IOperator<TOperation, TCandidate>> sources,
        IMeasurement<OperatorObservation<TOperation, TCandidate>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        Trace(sources, observation => aggregation.Aggregate(measurement.Read(observation), observation.Problem.Objective, objectiveComparer),
            clocks, retention);

    /// <summary>Traces the aggregate of the values measured in each call of the operator, using the observed run's objective.</summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TValue, TResult>(
        IOperator<TOperation, TCandidate> source,
        Func<OperatorObservation<TOperation, TCandidate>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        Trace([source], new DelegateMeasurement<OperatorObservation<TOperation, TCandidate>, TValue>(measure), aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each call of any of the operators, using the observed run's
    /// objective.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TOperation, TCandidate, TValue, TResult>(
        IReadOnlyList<IOperator<TOperation, TCandidate>> sources,
        Func<OperatorObservation<TOperation, TCandidate>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TOperation : IOperationDescriptor<TOperation> =>
        Trace(sources, new DelegateMeasurement<OperatorObservation<TOperation, TCandidate>, TValue>(measure), aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each search state the interceptor returns, using the observed
    /// run's objective.
    /// </summary>
    /// <remarks>A measurement of search states, such as <see cref="ObjectiveVectorsMeasurement{TCandidate}"/>, reads interceptors and algorithms alike.</remarks>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IOperator<Interception<TSearchState>, TCandidate> interceptor,
        IMeasurement<IStateObservation<TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace([interceptor], measurement, aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each search state any of the interceptors returns, using the
    /// observed run's objective.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IReadOnlyList<IOperator<Interception<TSearchState>, TCandidate>> interceptors,
        IMeasurement<IStateObservation<TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(interceptors, new DelegateMeasurement<OperatorObservation<Interception<TSearchState>, TCandidate>, TValue>(
                observation => measurement.Read(new InterceptedState<TSearchState>(observation.State))),
            aggregation, clocks, retention, objectiveComparer);

    /// <summary>Traces one value per search state the algorithm yields.</summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IAlgorithm<TCandidate, TSearchState> source,
        Func<AlgorithmObservation<TCandidate, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        Trace([source], value, clocks, retention);

    /// <summary>Traces one value per search state any of the algorithms yields.</summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> sources,
        Func<AlgorithmObservation<TCandidate, TSearchState>, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null)
        where TSearchState : class, ISearchState =>
        TraceAnalyzer<TResult>.Create(sources, static (source, record) => Aspect.Observe(source, record), value, clocks, retention);

    /// <summary>Traces the aggregate of the values measured in each search state the algorithm yields, using the observed run's objective.</summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IAlgorithm<TCandidate, TSearchState> source,
        IMeasurement<AlgorithmObservation<TCandidate, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace([source], measurement, aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each search state any of the algorithms yields, using the observed
    /// run's objective.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> sources,
        IMeasurement<AlgorithmObservation<TCandidate, TSearchState>, TValue> measurement,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(sources, observation => aggregation.Aggregate(measurement.Read(observation), observation.Problem.Objective, objectiveComparer),
            clocks, retention);

    /// <summary>Traces the aggregate of the values measured in each search state the algorithm yields, using the observed run's objective.</summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IAlgorithm<TCandidate, TSearchState> source,
        Func<AlgorithmObservation<TCandidate, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace([source], new DelegateMeasurement<AlgorithmObservation<TCandidate, TSearchState>, TValue>(measure), aggregation, clocks, retention, objectiveComparer);

    /// <summary>
    /// Traces the aggregate of the values measured in each search state any of the algorithms yields, using the observed
    /// run's objective.
    /// </summary>
    public static TraceAnalyzer<TResult> Trace<TCandidate, TSearchState, TValue, TResult>(
        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> sources,
        Func<AlgorithmObservation<TCandidate, TSearchState>, IReadOnlyList<TValue>> measure,
        IAggregation<TValue, TResult> aggregation, IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSearchState : class, ISearchState =>
        Trace(sources, new DelegateMeasurement<AlgorithmObservation<TCandidate, TSearchState>, TValue>(measure), aggregation, clocks, retention, objectiveComparer);

    // An interceptor observation is not a state observation itself, because the generic observation cannot implement an interface for one operation only.
    private sealed record InterceptedState<TSearchState>(TSearchState State) : IStateObservation<TSearchState>
        where TSearchState : class, ISearchState;
}
