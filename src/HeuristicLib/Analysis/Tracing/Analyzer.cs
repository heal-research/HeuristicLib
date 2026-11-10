using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Builds traces over the observations of algorithms and operators.</summary>
/// <remarks>
/// <para>
/// A trace reads one value from each observation through a function, such as
/// <c>crossover.Invocations.Trace(observation =&gt; observation.Offspring.Count)</c>. Counting, collecting and ranking are
/// ordinary functions in that form; ranking receives the call's own objective directions, as in
/// <c>evaluator.Invocations.Trace(observation =&gt; observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))</c>.
/// A value that keeps state across observations, such as a best so far, comes from an explicit state factory, which each run calls once.
/// </para>
/// <para>
/// A trace observes each boundary once, by its configuration and observation type, so a source given twice counts once.
/// Sources of different operations of one configuration are each observed.
/// </para>
/// <para>
/// A trace holds its functions, so a configuration it is placed in is not serializable.
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
    /// <param name="createState">Creates private accumulation state once per run.</param>
    /// <param name="value">Reads a value from that state and the observation.</param>
    /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
    /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
    public static TraceAnalyzer<TObservation, TValue> Trace<TObservation, TState, TValue>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<TState> createState, Func<TState, TObservation, TValue> value,
        IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) where TState : class =>
        new(sources, () =>
        {
            var state = createState();
            return observation => value(state, observation);
        }, clocks, retention);
}

/// <summary>Builds traces over the observations of one source.</summary>
/// <remarks>A trace holds its functions, so a configuration it is placed in is not serializable.</remarks>
public static class ObservationTraces
{
    extension<TObservation>(ObservationSource<TObservation> source)
    {
        /// <summary>Traces one value per observation of this source.</summary>
        public TraceAnalyzer<TObservation, TValue> Trace<TValue>(Func<TObservation, TValue> value,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) =>
            Analyzer.Trace([source], value, clocks, retention);

        /// <summary>Traces one value per observation of this source, with a value function created for each run.</summary>
        /// <param name="createState">Creates private accumulation state once per run.</param>
        /// <param name="value">Reads a value from that state and the observation.</param>
        /// <param name="clocks">The clocks each entry records. When omitted, entries record no time.</param>
        /// <param name="retention">Decides which observations become entries. When omitted, every observation does.</param>
        public TraceAnalyzer<TObservation, TValue> Trace<TState, TValue>(Func<TState> createState, Func<TState, TObservation, TValue> value,
            IReadOnlyList<IClock>? clocks = null, TraceRetention? retention = null) where TState : class =>
            Analyzer.Trace([source], createState, value, clocks, retention);
    }
}
