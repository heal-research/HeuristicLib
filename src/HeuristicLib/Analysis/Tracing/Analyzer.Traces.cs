using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Analysis;

public static partial class Analyzer
{
    /// <summary>
    /// Builds a trace over the distinct sources, storing what <paramref name="measure"/> returns for each observation.
    /// </summary>
    private static TraceAnalyzer<TResult> Create<TSource, TObservation, TResult>(
        IReadOnlyList<TSource> sources,
        Func<TSource, Action<TObservation>, IExecutionModule> createModule,
        IReadOnlyList<Clock>? clocks,
        TraceRetention? retention,
        Func<TObservation, TResult> measure)
        where TSource : class
    {
        var distinctSources = sources.Distinct<TSource>(ReferenceEqualityComparer.Instance).ToArray();
        if (distinctSources.Length == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");

        var trace = new TraceAnalyzer<TResult>(retention ?? TraceRetention.EveryObservation(), clocks ?? []);
        trace.SetObservationModules(distinctSources.Select(source =>
            createModule(source, observation => trace.Record(measure, observation))));
        return trace;
    }

    /// <summary>
    /// Builds a trace whose stored value is what the aggregation makes of the measurement, ranked by the observed
    /// run's objective unless a comparer is given.
    /// </summary>
    private static TraceAnalyzer<TResult> Create<TSource, TObservation, TProblem, TValue, TResult>(
        IReadOnlyList<TSource> sources,
        Func<TSource, Action<TObservation>, IExecutionModule> createModule,
        IReadOnlyList<Clock>? clocks,
        TraceRetention? retention,
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IComparer<ObjectiveVector>? objectiveComparer)
        where TSource : class
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        Create(sources, createModule, clocks, retention,
            observation => aggregation.Aggregate(measurement.Read(observation), observation.Problem.Objective, objectiveComparer));
}
