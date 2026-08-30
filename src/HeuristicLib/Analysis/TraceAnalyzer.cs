using System.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// A run-bound analyzer that records a trace of aggregated values and when they were observed.
/// </summary>
/// <remarks>
/// Properties publish already collected values without allocating. Snapshots and projections allocate immutable values
/// that stay unchanged while the run continues, during and after execution.
/// </remarks>
public sealed class TraceAnalyzer<TResult> : IAnalyzer, IDisposable
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<TResult>> entries = [];
    private readonly ImmutableArray<Clock> clocks;
    private readonly Action<ExecutionInstanceRegistry, TraceAnalyzer<TResult>> install;
    private readonly IRetention<TResult> retention;
    private bool isCompleted;

    internal TraceAnalyzer(Action<ExecutionInstanceRegistry, TraceAnalyzer<TResult>> install, IRetention<TResult> retention, IReadOnlyList<Clock> clocks)
    {
        this.install = install;
        this.retention = retention;
        this.clocks = [.. clocks];
    }

    public int SampleCount { get { lock (sync) return entries.Count; } }

    public TraceEntry<TResult>? Latest { get { lock (sync) return entries.Count == 0 ? null : entries[^1]; } }

    public bool IsCompleted { get { lock (sync) return isCompleted; } }

    public TraceSnapshot<TResult> Snapshot()
    {
        lock (sync)
            return new TraceSnapshot<TResult>([.. entries]);
    }

    /// <summary>
    /// Projects the trace onto time read from one of its clocks.
    /// </summary>
    public IReadOnlyList<TracePoint<TTime, TResult>> By<TTime>(Clock<TTime> clock)
    {
        if (!clocks.Any(selected => ReferenceEquals(selected, clock)))
            throw new InvalidOperationException("The trace does not use the requested clock.");

        return [.. Snapshot().Select(entry => new TracePoint<TTime, TResult>(entry.At(clock), entry.Value))];
    }

    internal void Record(TResult value, ObjectiveDirections objective)
    {
        lock (sync)
        {
            if (isCompleted)
                throw new InvalidOperationException("A completed analyzer cannot record another sample.");

            if (!retention.ShouldRecord(value, objective))
                return;

            entries.Add(new TraceEntry<TResult>(Moment.Read(clocks), value));
        }
    }

    /// <summary>
    /// Installs the clocks before the recorder, so that every clock has advanced by the time a moment is captured.
    /// </summary>
    public void Install(ExecutionInstanceRegistry registry)
    {
        foreach (var clock in clocks)
            clock.Install(registry);

        install(registry, this);
    }

    public void Dispose()
    {
        lock (sync)
            isCompleted = true;
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

    internal TraceSnapshot(ImmutableArray<TraceEntry<TValue>> entries)
    {
        this.entries = entries;
    }

    public int Count => entries.Length;
    public TraceEntry<TValue> this[int index] => entries[index];
    public ImmutableArray<TraceEntry<TValue>>.Enumerator GetEnumerator() => entries.GetEnumerator();
    IEnumerator<TraceEntry<TValue>> IEnumerable<TraceEntry<TValue>>.GetEnumerator() =>
        ((IEnumerable<TraceEntry<TValue>>)entries).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries).GetEnumerator();
}
