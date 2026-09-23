using System.Collections;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// A mutable trace with immutable snapshots. Sharing this object intentionally shares its history.
/// </summary>
/// <remarks>
/// What a trace measures is a function of the observation, supplied when <see cref="Analyzer"/> builds one. The trace
/// itself only stores what that function returned, so measuring and aggregating need no trace type of their own.
/// </remarks>
public sealed class TraceAnalyzer<TResult> : IAnalyzer
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<TResult>> entries = [];
    private readonly ImmutableArray<Clock> clocks;
    private readonly TraceRetention retention;
    private ImmutableArray<IExecutionModule> observationModules = [];

    internal TraceAnalyzer(TraceRetention retention, IReadOnlyList<Clock> clocks)
    {
        this.retention = retention;
        this.clocks = [.. clocks.Distinct<Clock>(ReferenceEqualityComparer.Instance)];
    }

    public int SampleCount { get { lock (sync) return entries.Count; } }
    public TraceEntry<TResult>? Latest { get { lock (sync) return entries.Count == 0 ? null : entries[^1]; } }

    public TResult RequireLatestValue()
    {
        lock (sync)
            return entries.Count == 0
                ? throw new InvalidOperationException("The trace has not retained any values.")
                : entries[^1].Value;
    }

    public TraceSnapshot<TResult> Snapshot()
    {
        lock (sync)
            return new TraceSnapshot<TResult>([.. entries], clocks);
    }

    public IReadOnlyList<TracePoint<TTime, TResult>> By<TTime>(Clock<TTime> clock) => Snapshot().By(clock);

    /// <summary>
    /// Measures and stores under one lock. An observation of one source arrives on the thread that called the
    /// operator, so the lock is not about parallel evaluation: it is what lets a caller read the trace while a run
    /// writes to it, and what keeps a shared analyzer consistent across runs executing at the same time. The
    /// observation is passed alongside the function so the callback allocates nothing.
    /// </summary>
    internal void Record<TObservation>(Func<TObservation, TResult> measure, TObservation observation)
    {
        lock (sync)
        {
            var value = measure(observation);
            var decision = retention.Decide(value);
            switch (decision)
            {
                case RetentionDecision.Append:
                    entries.Add(new TraceEntry<TResult>(Moment.Read(clocks), value));
                    break;
                case RetentionDecision.ReplaceLatest:
                    var entry = new TraceEntry<TResult>(Moment.Read(clocks), value);
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

    internal void SetObservationModules(IEnumerable<IExecutionModule> modules)
    {
        if (!observationModules.IsEmpty)
            throw new InvalidOperationException("A trace analyzer's observation sources can only be assigned once.");
        observationModules = [.. modules];
    }

    /// <summary>Installs clocks before observations. Installation never resets collected state.</summary>
    public void Install(ResolutionScopeBuilder builder)
    {
        foreach (var clock in clocks)
            clock.Install(builder);
        foreach (var module in observationModules)
            builder.Install(module);
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
