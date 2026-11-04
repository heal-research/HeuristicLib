using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Builds traces over the observations of algorithms and operators.</summary>
/// <remarks>
/// <para>
/// A trace reads one value from each observation through a function, such as
/// <c>crossover.Invocations.Trace(observation =&gt; observation.Offspring.Count)</c>. Counting, collecting and ranking are
/// ordinary functions in that form; ranking receives the call's own objective directions, as in
/// <c>evaluator.Invocations.Trace(observation =&gt; observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))</c>.
/// A value that keeps state across observations, such as a best so far, comes from a function that creates the value
/// function, which each run calls once.
/// </para>
/// <para>
/// A trace observes each boundary once, by its configuration and observation type, so a source given twice counts once.
/// Sources of different operations of one configuration are each observed.
/// </para>
/// </remarks>
public static class Analyzer
{
    /// <summary>Traces one value per observation of any of the sources.</summary>
    public static TraceAnalyzer<TObservation, TValue> Trace<TObservation, TValue>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<TObservation, TValue> value,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
        new(sources, () => value, clocks, retention);

    /// <summary>Traces one value per observation of any of the sources, with a value function created for each run.</summary>
    /// <param name="sources">The observed sources; a configuration given twice is observed once.</param>
    /// <param name="createValue">Creates the value function at the start of each run, so the state it keeps belongs to that run.</param>
    /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
    /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
    public static TraceAnalyzer<TObservation, TValue> Trace<TObservation, TValue>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<Func<TObservation, TValue>> createValue,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
        new(sources, createValue, clocks, retention);
}

public static class ObservationTraces
{
    extension<TObservation>(ObservationSource<TObservation> source)
    {
        /// <summary>Traces one value per observation of this source.</summary>
        public TraceAnalyzer<TObservation, TValue> Trace<TValue>(Func<TObservation, TValue> value,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
            Analyzer.Trace([source], value, clocks, retention);

        /// <summary>Traces one value per observation of this source, with a value function created for each run.</summary>
        /// <param name="createValue">Creates the value function at the start of each run, so the state it keeps belongs to that run.</param>
        /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
        /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
        public TraceAnalyzer<TObservation, TValue> Trace<TValue>(Func<Func<TObservation, TValue>> createValue,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
            Analyzer.Trace([source], createValue, clocks, retention);
    }
}
