using System.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Defines a trace: the values one function reads from each observation of its sources, recorded against clocks.</summary>
/// <remarks>
/// <para>
/// The analyzer holds immutable settings only: its sources, clocks, retention and value function.
/// Each run creates its own <see cref="Trace{TValue}"/>, read with <c>run.Result(trace)</c>, so reusing one
/// analyzer for several runs gives each run an independent trace. Attach it with <c>CreateRun</c>, which wraps its
/// sources wherever the configuration holds them, or place it explicitly with an <c>Observed</c> wrapper, which records
/// into the same result. Value functions are delegates, so a configuration holding the analyzer is not persistable.
/// </para>
/// <para>
/// A trace observes each boundary once, by its configuration and observation type, so a source given twice counts
/// once. Clocks only read; the caller places any clock that needs placement, or its counter's or duration's placement,
/// after every analyzer that reads it in one list, so its reading includes the call. Listed before them, it reads before
/// the call. The start of an algorithm call is the exception: it is reported before the wrappers inside it run, so it
/// reads a counter of that algorithm's calls before its own call is counted.
/// </para>
/// </remarks>
public sealed class TraceAnalyzer<TObservation, TValue> : Analyzer<Trace<TValue>>, IObservationReceiver<TObservation>
{
    private readonly ValueArray<ObservationSource<TObservation>> sources;
    private readonly ValueArray<IClock> clocks;
    private readonly TraceRetention retention;
    private readonly Func<Func<TObservation, TValue>> createValue;

    internal TraceAnalyzer(IReadOnlyList<ObservationSource<TObservation>> sources, Func<Func<TObservation, TValue>> createValue,
        IReadOnlyList<IClock>? clocks, TraceRetention? retention)
    {
        // Sources of one observation type observe one boundary per configuration reference.
        this.sources = [.. sources.DistinctBy(static source => source.Configuration, ReferenceEqualityComparer.Instance)];
        if (this.sources.Count == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");
        this.clocks = [.. (clocks ?? []).Distinct()];
        this.retention = retention ?? TraceRetention.EveryObservation();
        this.createValue = createValue;
    }

    /// <summary>Lists only source recordings; clocks place nothing through this trace.</summary>
    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
        [.. sources.Select(source => source.RecordingInto(this))];

    /// <summary>Creates the callback that records one observation into this run's trace.</summary>
    /// <remarks>Value computation, retention and publication are serialized by the result.</remarks>
    /// <param name="results">The result table of the run being observed.</param>
    public Action<TObservation> CreateRecorder(RunResults results) => ((Trace<TObservation, TValue>)results.Result(this)).Record;

    /// <inheritdoc/>
    public override Trace<TValue> CreateExecutionNode(ResolutionScope scope) =>
        new Trace<TObservation, TValue>(createValue(), [.. clocks], [.. clocks.Select(clock => scope.Results.Result(clock))], retention.CreateDecider<TValue>());
}

/// <summary>A run's trace: the entries a trace analyzer recorded, read through immutable snapshots.</summary>
/// <remarks>
/// The trace is safe to read while the run writes to it. It belongs to one run; the analyzer that defines it creates a
/// new trace for every run, spanning all Cycle turns. Values must be immutable; snapshots retain their references.
/// Entries follow collection order, which need not be deterministic for concurrent observations. Results remain
/// readable after completion, cancellation or failure.
/// </remarks>
public abstract class Trace<TValue> : IRunResult, IEnumerable<TraceEntry<TValue>>
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<TValue>> entries = [];
    private readonly RetentionDecider<TValue> decider;
    private readonly IClock[] columns;
    private readonly IClockReading[] readings;

    private protected Trace(IClock[] columns, IClockReading[] readings, RetentionDecider<TValue> decider)
    {
        this.columns = columns;
        this.readings = readings;
        this.decider = decider;
    }

    /// <summary>Enumerates a snapshot taken when this method is called.</summary>
    public IEnumerator<TraceEntry<TValue>> GetEnumerator() => ((IEnumerable<TraceEntry<TValue>>)Snapshot()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>The number of retained entries, rather than the number of observations.</summary>
    public int SampleCount { get { lock (sync) return entries.Count; } }
    /// <summary>The last retained entry, or <see langword="null"/> before any entry is retained.</summary>
    public TraceEntry<TValue>? Latest { get { lock (sync) return entries.Count == 0 ? null : entries[^1]; } }

    /// <summary>Reads the value of the last retained entry.</summary>
    /// <exception cref="InvalidOperationException">The trace has retained no entry.</exception>
    public TValue RequireLatestValue()
    {
        lock (sync)
            return entries.Count == 0
                ? throw new InvalidOperationException("The trace has not retained any values.")
                : entries[^1].Value;
    }

    /// <summary>Copies the retained entries in collection order, keeping value references.</summary>
    public TraceSnapshot<TValue> Snapshot()
    {
        lock (sync)
            return new TraceSnapshot<TValue>([.. entries], columns);
    }

    /// <summary>Copies the retained values and their recorded readings of the clock, in collection order.</summary>
    /// <param name="clock">A clock equal to one supplied when defining the analyzer.</param>
    /// <exception cref="InvalidOperationException">The trace does not record this clock.</exception>
    public IReadOnlyList<TracePoint<TTime, TValue>> By<TTime>(IClock<TTime> clock) => Snapshot().By(clock);

    /// <summary>
    /// Serializes value computation, retention and publication with other observations and live reads.
    /// Retention never skips value computation; clock readings are taken only for retained values.
    /// </summary>
    private protected void Record<TObservation>(Func<TObservation, TValue> read, TObservation observation)
    {
        lock (sync)
        {
            var value = read(observation);
            var decision = decider.Decide(value);
            switch (decision)
            {
                case RetentionDecision.Append:
                    entries.Add(new TraceEntry<TValue>(Moment.Read(columns, readings), value));
                    break;
                case RetentionDecision.ReplaceLatest:
                    var entry = new TraceEntry<TValue>(Moment.Read(columns, readings), value);
                    if (entries.Count == 0)
                        entries.Add(entry);
                    else
                        entries[^1] = entry;
                    break;
                case RetentionDecision.Skip:
                    break;
                default:
                    throw new InvalidOperationException($"The retention returned the unknown decision '{decision}'.");
            }
        }
    }
}

/// <summary>A run's trace of the values read from observations of the type <typeparamref name="TObservation"/>.</summary>
internal sealed class Trace<TObservation, TValue> : Trace<TValue>
{
    private readonly Func<TObservation, TValue> value;

    internal Trace(Func<TObservation, TValue> value, IClock[] columns, IClockReading[] readings, RetentionDecider<TValue> decider)
        : base(columns, readings, decider)
    {
        this.value = value;
    }

    /// <summary>Reads the value of one observation and records it as the trace's retention decides.</summary>
    public void Record(TObservation observation) => Record(value, observation);
}

/// <summary>A retained value with the clock readings taken when it was recorded.</summary>
public readonly record struct TraceEntry<TValue>
{
    private readonly Moment moment;

    internal TraceEntry(Moment moment, TValue value)
    {
        this.moment = moment;
        Value = value;
    }

    public TValue Value { get; }

    /// <summary>Returns the recorded reading of a clock supplied to the analyzer.</summary>
    /// <param name="clock">A clock equal to one supplied when defining the analyzer.</param>
    /// <exception cref="InvalidOperationException">The trace does not record this clock.</exception>
    public TTime At<TTime>(IClock<TTime> clock) => moment.At(clock);
}

/// <summary>A retained trace value paired with one of its recorded clock readings.</summary>
public readonly record struct TracePoint<TTime, TValue>(TTime Time, TValue Value);

/// <summary>A stable copy of retained trace entries in collection order; value references are not copied.</summary>
public sealed class TraceSnapshot<TValue> : IReadOnlyList<TraceEntry<TValue>>
{
    private readonly ImmutableArray<TraceEntry<TValue>> entries;
    private readonly IClock[] columns;

    internal TraceSnapshot(ImmutableArray<TraceEntry<TValue>> entries, IClock[] columns)
    {
        this.entries = entries;
        this.columns = columns;
    }

    /// <summary>Copies values paired with their recorded clock readings, preserving entry order.</summary>
    /// <param name="clock">A clock equal to one supplied when defining the analyzer.</param>
    /// <exception cref="InvalidOperationException">The trace does not record this clock, even if the snapshot is empty.</exception>
    public IReadOnlyList<TracePoint<TTime, TValue>> By<TTime>(IClock<TTime> clock)
    {
        if (!columns.Any(selected => Equals(selected, clock)))
            throw new InvalidOperationException("The trace does not use the requested clock.");
        return entries.Select(entry => new TracePoint<TTime, TValue>(entry.At(clock), entry.Value)).ToImmutableArray();
    }

    public int Count => entries.Length;
    /// <summary>Copies the values in entry order, retaining their references.</summary>
    public ImmutableArray<TValue> Values => entries.Select(entry => entry.Value).ToImmutableArray();
    public TraceEntry<TValue> this[int index] => entries[index];
    public ImmutableArray<TraceEntry<TValue>>.Enumerator GetEnumerator() => entries.GetEnumerator();
    IEnumerator<TraceEntry<TValue>> IEnumerable<TraceEntry<TValue>>.GetEnumerator() =>
        ((IEnumerable<TraceEntry<TValue>>)entries).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries).GetEnumerator();
}
