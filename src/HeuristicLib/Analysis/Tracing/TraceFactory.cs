using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    private static TraceAnalyzer<TResult> CreateAggregating<TSource, TObservation, TProblem, TValue, TResult>(
        IReadOnlyList<TSource> sources,
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        Func<TSource, Action<TObservation>, IExecutionHook> createHook,
        IReadOnlyList<Clock>? clocks,
        TraceRetention? retention,
        IComparer<ObjectiveVector>? objectiveComparer)
        where TSource : class
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem
    {
        var distinctSources = sources.Distinct<TSource>(ReferenceEqualityComparer.Instance).ToArray();
        if (distinctSources.Length == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");

        var resolver = ExecutionInstanceResolver.Create();
        var trace = new AggregatingTrace<TObservation, TProblem, TValue, TResult>(measurement,
            resolver.Resolve(aggregation), resolver.Resolve(retention ?? TraceRetention.EveryObservation()), clocks ?? [], objectiveComparer);
        trace.SetObservationHooks(distinctSources.Select(source => createHook(source, trace.Record)));
        return trace;
    }

    private static TraceAnalyzer<TResult> CreateProjected<TSource, TObservation, TResult>(
        IReadOnlyList<TSource> sources,
        Func<TObservation, TResult> projection,
        Func<TSource, Action<TObservation>, IExecutionHook> createHook,
        IReadOnlyList<Clock>? clocks,
        TraceRetention? retention)
        where TSource : class
        where TObservation : Observation
    {
        var distinctSources = sources.Distinct<TSource>(ReferenceEqualityComparer.Instance).ToArray();
        if (distinctSources.Length == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");

        var trace = new ProjectedTrace<TObservation, TResult>(projection,
            ExecutionInstanceResolver.Create().Resolve(retention ?? TraceRetention.EveryObservation()), clocks ?? []);
        trace.SetObservationHooks(distinctSources.Select(source => createHook(source, trace.Record)));
        return trace;
    }
}

internal sealed class AggregatingTrace<TObservation, TProblem, TValue, TResult>(
    IMeasurement<TObservation, TValue> measurement,
    IAggregationInstance<TValue, TResult> aggregation,
    ITraceRetentionInstance retention,
    IReadOnlyList<Clock> clocks,
    IComparer<ObjectiveVector>? objectiveComparer)
    : TraceAnalyzer<TResult>(retention, clocks)
    where TObservation : Observation<TProblem>
    where TProblem : class, IProblem
{
    internal void Record(TObservation observation)
    {
        lock (Sync)
            RecordValue(aggregation.Aggregate(measurement.Read(observation), observation.Problem.Objective, objectiveComparer));
    }
}

internal sealed class ProjectedTrace<TObservation, TResult>(
    Func<TObservation, TResult> projection,
    ITraceRetentionInstance retention,
    IReadOnlyList<Clock> clocks)
    : TraceAnalyzer<TResult>(retention, clocks)
    where TObservation : Observation
{
    internal void Record(TObservation observation)
    {
        lock (Sync)
            RecordValue(projection(observation));
    }
}
