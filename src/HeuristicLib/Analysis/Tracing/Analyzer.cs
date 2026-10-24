using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Builds traces over the observations of algorithms and operators.</summary>
/// <remarks>
/// <para>
/// A trace reads one value from each observation through a function, such as
/// <c>Analyzer.Trace(crossover.Calls, observation =&gt; observation.Offspring.Count)</c>. Counting, collecting and ranking are ordinary
/// functions in that form; ranking receives the call's own objective directions, as in
/// <c>Analyzer.Trace(evaluator.Calls, observation =&gt; observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective))</c>.
/// </para>
/// <para>
/// A trace observes each boundary once, by its configuration and observation type, so a source given twice counts once.
/// Sources of different operations of one configuration are each observed.
/// </para>
/// </remarks>
public static class Analyzer
{
    /// <summary>Traces one value per observation.</summary>
    public static TraceAnalyzer<TResult> Trace<TObservation, TResult>(
        ObservationSource<TObservation> source,
        Func<TObservation, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        Trace([source], value, clocks, retention);

    /// <summary>Traces one value per observation of any of the sources.</summary>
    public static TraceAnalyzer<TResult> Trace<TObservation, TResult>(
        IReadOnlyList<ObservationSource<TObservation>> sources,
        Func<TObservation, TResult> value,
        IReadOnlyList<Clock>? clocks = null, TraceRetention? retention = null) =>
        TraceAnalyzer<TResult>.Create(sources, value, clocks, retention);
}
