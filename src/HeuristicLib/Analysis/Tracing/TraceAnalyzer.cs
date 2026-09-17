using System.Collections;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>A mutable trace with immutable snapshots. Sharing this object intentionally shares its history.</summary>
public abstract class TraceAnalyzer<TResult> : IAnalyzer
{
    protected Lock Sync { get; } = new();
    private readonly List<TraceEntry<TResult>> entries = [];
    private readonly ImmutableArray<Clock> clocks;
    private readonly ITraceRetentionInstance retention;
    private ImmutableArray<IExecutionHook> observationHooks = [];

    protected TraceAnalyzer(ITraceRetentionInstance retention, IReadOnlyList<Clock> clocks)
    {
        this.retention = retention;
        this.clocks = [.. clocks.Distinct<Clock>(ReferenceEqualityComparer.Instance)];
    }

    public int SampleCount { get { lock (Sync) return entries.Count; } }
    public TraceEntry<TResult>? Latest { get { lock (Sync) return entries.Count == 0 ? null : entries[^1]; } }

    public TResult RequireLatestValue()
    {
        lock (Sync)
            return entries.Count == 0
                ? throw new InvalidOperationException("The trace has not retained any values.")
                : entries[^1].Value;
    }

    public TraceSnapshot<TResult> Snapshot()
    {
        lock (Sync)
            return new TraceSnapshot<TResult>([.. entries], clocks);
    }

    public IReadOnlyList<TracePoint<TTime, TResult>> By<TTime>(Clock<TTime> clock) => Snapshot().By(clock);

    protected void RecordValue(TResult value)
    {
        lock (Sync)
        {
            if (retention.ShouldRetain(value))
                entries.Add(new TraceEntry<TResult>(Moment.Read(clocks), value));
        }
    }

    internal void SetObservationHooks(IEnumerable<IExecutionHook> hooks)
    {
        if (!observationHooks.IsEmpty)
            throw new InvalidOperationException("A trace analyzer's observation sources can only be assigned once.");
        observationHooks = [.. hooks];
    }

    /// <summary>Installs clocks before observations. Installation never resets collected state.</summary>
    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var clock in clocks)
            clock.Install(builder);
        foreach (var hook in observationHooks)
            builder.Install(hook);
    }
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

    public TTime At<TTime>(Clock<TTime> clock) => moment.At(clock);
}

public readonly record struct TracePoint<TTime, TValue>(TTime Time, TValue Value);

public sealed class TraceSnapshot<TValue> : IReadOnlyList<TraceEntry<TValue>>
{
    private readonly ImmutableArray<TraceEntry<TValue>> entries;
    private readonly ImmutableArray<Clock> clocks;

    internal TraceSnapshot(ImmutableArray<TraceEntry<TValue>> entries, ImmutableArray<Clock> clocks)
    {
        this.entries = entries;
        this.clocks = clocks;
    }

    public IReadOnlyList<TracePoint<TTime, TValue>> By<TTime>(Clock<TTime> clock)
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
