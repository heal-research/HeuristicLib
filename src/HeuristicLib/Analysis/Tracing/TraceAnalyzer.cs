using System.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// A mutable trace with immutable snapshots. Sharing this object intentionally shares its history.
/// </summary>
/// <remarks>
/// What a trace measures is a function of the observation, supplied when <see cref="Analyzer"/> builds one. The trace
/// itself only stores what that function returned, so measuring and aggregating need no trace type of their own.
/// </remarks>
public sealed class TraceAnalyzer<TResult> : Aspect
{
    private readonly Lock sync = new();
    private readonly List<TraceEntry<TResult>> entries = [];
    private readonly TraceRetention retention;
    private readonly ImmutableArray<Clock> clocks;

    private TraceAnalyzer(TraceRetention retention, IReadOnlyList<Clock> clocks, Func<TraceAnalyzer<TResult>, ImmutableArray<Aspect>> createObservationAspects)
    {
        this.retention = retention;
        this.clocks = [.. clocks.Distinct<Clock>(ReferenceEqualityComparer.Instance)];
        // Storage is initialized before observer callbacks capture this trace. Clock advice precedes the observations,
        // so an entry reads clocks that already include its call.
        Advices = [.. this.clocks.SelectMany(clock => clock.Advices), .. createObservationAspects(this).SelectMany(aspect => aspect.Advices)];
    }

    /// <summary>Builds a trace over distinct sources, storing the measured value of each observation.</summary>
    /// <remarks>
    /// The aspect factory connects each source to the supplied recording callback. This runtime construction API
    /// supports custom observation roles; source identity and shared clocks use reference equality.
    /// </remarks>
    public static TraceAnalyzer<TResult> Create<TSource, TObservation>(
        IReadOnlyList<TSource> sources,
        Func<TSource, Action<TObservation>, Aspect> createAspect,
        Func<TObservation, TResult> measure,
        IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null)
        where TSource : class
    {
        var distinctSources = sources.Distinct<TSource>(ReferenceEqualityComparer.Instance).ToArray();
        if (distinctSources.Length == 0)
            throw new InvalidOperationException("A trace analyzer requires at least one observation source.");

        return new TraceAnalyzer<TResult>(retention ?? TraceRetention.EveryObservation(), clocks ?? [],
            trace => [.. distinctSources.Select(source =>
                createAspect(source, observation => trace.Record(measure, observation)))]);
    }

    /// <summary>Builds a trace of aggregated measurements using the observed problem's objective.</summary>
    public static TraceAnalyzer<TResult> Create<TSource, TObservation, TProblem, TValue>(
        IReadOnlyList<TSource> sources,
        Func<TSource, Action<TObservation>, Aspect> createAspect,
        IMeasurement<TObservation, TValue> measurement,
        IAggregation<TValue, TResult> aggregation,
        IReadOnlyList<Clock>? clocks = null,
        TraceRetention? retention = null,
        IComparer<ObjectiveVector>? objectiveComparer = null)
        where TSource : class
        where TObservation : Observation<TProblem>
        where TProblem : class, IProblem =>
        Create(sources, createAspect,
            observation => aggregation.Aggregate(measurement.Read(observation), observation.Problem.Objective, objectiveComparer),
            clocks, retention);

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
    private void Record<TObservation>(Func<TObservation, TResult> measure, TObservation observation)
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
