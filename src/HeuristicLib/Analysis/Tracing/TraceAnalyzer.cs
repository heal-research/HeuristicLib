using System.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Defines a trace: the values one function reads from each observation of its sources, recorded against clocks.</summary>
/// <remarks>
/// <para>
/// The analyzer is an instrument and holds immutable settings only: its sources, clocks, retention and value function.
/// Each run creates its own <see cref="Trace{TObservation, TValue}"/>, read with <c>run.Result(trace)</c>, so reusing one
/// analyzer for several runs gives each run an independent trace. Attach it with <c>Advised</c>, which observes its
/// sources wherever they are resolved, or place it explicitly with an <c>Observed</c> wrapper, which records into the
/// same result.
/// </para>
/// <para>
/// A trace observes each boundary once, by its configuration and observation type, so a source given twice counts
/// once. Attaching it also attaches its clocks, closer to the executors than its observations, so an observation made
/// after a call, such as an invocation or a yielded state, reads clocks that already include that call. The start of an
/// algorithm call is the exception: it is reported before the wrappers inside it run, so it reads a counter of that
/// algorithm's calls before its own call is counted.
/// </para>
/// </remarks>
public sealed class TraceAnalyzer<TObservation, TValue> : Instrument<Trace<TObservation, TValue>>, IObservationInstrument<TObservation>
{
    private readonly ImmutableArray<ObservationSource<TObservation>> sources;
    private readonly ImmutableArray<IClock> clocks;
    private readonly TraceRetention retention;
    private readonly Func<Func<TObservation, TValue>> createValue;

    internal TraceAnalyzer(IReadOnlyList<ObservationSource<TObservation>> sources, Func<Func<TObservation, TValue>> createValue,
        IReadOnlyList<IClock>? clocks, TraceRetention? retention)
    {
        // Sources of one observation type observe one boundary per configuration reference.
        this.sources = [.. sources.DistinctBy(static source => source.Configuration, ReferenceEqualityComparer.Instance)];
        if (this.sources.Length == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");
        this.clocks = [.. (clocks ?? []).Distinct<IClock>(ReferenceEqualityComparer.Instance)];
        this.retention = retention ?? TraceRetention.EveryObservation();
        this.createValue = createValue;
    }

    /// <summary>Creates the callback that records one observation into this run's trace.</summary>
    public Action<TObservation> CreateRecorder(RunResults results) => results.Result(this).Record;

    protected override Trace<TObservation, TValue> CreateResult(RunResults results) =>
        new(createValue(), [.. clocks.Select(clock => (clock, clock.Bind(results).Read))], retention.CreateDecider<TValue>());

    // Clock advice comes first, so it wraps closer and an entry reads clocks that already include its call.
    protected override Aspect CreateAspect(Trace<TObservation, TValue> result, RunResults results) =>
        new Observing([.. clocks.Select(clock => clock.Bind(results).Aspect), .. sources.Select(source => Aspect.Observe(source, result.Record))]);

    private sealed class Observing : Aspect
    {
        public Observing(IEnumerable<Aspect> aspects)
        {
            Advices = [.. aspects.SelectMany(aspect => aspect)];
        }
    }
}

/// <summary>A run's trace: the entries a trace analyzer recorded, read through immutable snapshots.</summary>
/// <remarks>
/// The trace is safe to read while the run writes to it. It belongs to one run; the analyzer that defines it creates a
/// new trace for every run.
/// </remarks>
public abstract class Trace<TValue>
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<TValue>> entries = [];
    private readonly ImmutableArray<(IClock Clock, Func<object> Read)> clocks;
    private readonly RetentionDecider<TValue> decider;

    private protected Trace(ImmutableArray<(IClock Clock, Func<object> Read)> clocks, RetentionDecider<TValue> decider)
    {
        this.clocks = clocks;
        this.decider = decider;
    }

    public int SampleCount { get { lock (sync) return entries.Count; } }
    public TraceEntry<TValue>? Latest { get { lock (sync) return entries.Count == 0 ? null : entries[^1]; } }

    public TValue RequireLatestValue()
    {
        lock (sync)
            return entries.Count == 0
                ? throw new InvalidOperationException("The trace has not retained any values.")
                : entries[^1].Value;
    }

    public TraceSnapshot<TValue> Snapshot()
    {
        lock (sync)
            return new TraceSnapshot<TValue>([.. entries], [.. clocks.Select(clock => clock.Clock)]);
    }

    public IReadOnlyList<TracePoint<TTime, TValue>> By<TTime>(IClock<TTime> clock) => Snapshot().By(clock);

    /// <summary>
    /// Reads and stores under one lock. An observation of one source arrives on the thread that called the
    /// operator, so the lock is not about parallel evaluation: it is what lets a caller read the trace while a run
    /// writes to it. The value is read under the lock too, so a value function that keeps state, such as a best so far,
    /// sees the observations one at a time.
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
                    entries.Add(new TraceEntry<TValue>(Moment.Read(clocks), value));
                    break;
                case RetentionDecision.ReplaceLatest:
                    var entry = new TraceEntry<TValue>(Moment.Read(clocks), value);
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
public sealed class Trace<TObservation, TValue> : Trace<TValue>
{
    private readonly Func<TObservation, TValue> value;

    internal Trace(Func<TObservation, TValue> value, ImmutableArray<(IClock Clock, Func<object> Read)> clocks, RetentionDecider<TValue> decider)
        : base(clocks, decider)
    {
        this.value = value;
    }

    /// <summary>Reads the value of one observation and records it as the trace's retention decides.</summary>
    public void Record(TObservation observation) => Record(value, observation);
}

public readonly record struct TraceEntry<TValue>
{
    private readonly Moment moment;

    internal TraceEntry(Moment moment, TValue value)
    {
        this.moment = moment;
        Value = value;
    }

    public TValue Value { get; }

    public TTime At<TTime>(IClock<TTime> clock) => moment.At(clock);
}

public readonly record struct TracePoint<TTime, TValue>(TTime Time, TValue Value);

public sealed class TraceSnapshot<TValue> : IReadOnlyList<TraceEntry<TValue>>
{
    private readonly ImmutableArray<TraceEntry<TValue>> entries;
    private readonly ImmutableArray<IClock> clocks;

    internal TraceSnapshot(ImmutableArray<TraceEntry<TValue>> entries, ImmutableArray<IClock> clocks)
    {
        this.entries = entries;
        this.clocks = clocks;
    }

    public IReadOnlyList<TracePoint<TTime, TValue>> By<TTime>(IClock<TTime> clock)
    {
        if (!clocks.Any(selected => ReferenceEquals(selected, clock)))
            throw new InvalidOperationException("The trace does not use the requested clock.");
        return entries.Select(entry => new TracePoint<TTime, TValue>(entry.At(clock), entry.Value)).ToImmutableArray();
    }

    public int Count => entries.Length;
    public ImmutableArray<TValue> Values => entries.Select(entry => entry.Value).ToImmutableArray();
    public TraceEntry<TValue> this[int index] => entries[index];
    public ImmutableArray<TraceEntry<TValue>>.Enumerator GetEnumerator() => entries.GetEnumerator();
    IEnumerator<TraceEntry<TValue>> IEnumerable<TraceEntry<TValue>>.GetEnumerator() =>
        ((IEnumerable<TraceEntry<TValue>>)entries).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries).GetEnumerator();
}
