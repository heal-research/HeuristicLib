using System.Collections;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>What a trace does with one aggregated observation.</summary>
public enum RetentionDecision
{
    /// <summary>Store the value as a new entry. This is the value an unset decision has.</summary>
    Append,

    /// <summary>Store the value over the entry before it, so the trace keeps no history of it.</summary>
    ReplaceLatest,

    /// <summary>Drop the value. The entries already stored are untouched.</summary>
    Skip
}

/// <summary>
/// Decides what a trace does with each aggregated observation.
/// </summary>
/// <remarks>
/// A retention is an immutable policy. What it counts or remembers belongs to the <see cref="RetentionDecider{TValue}"/>
/// it creates for each trace, so traces sharing one retention, and the runs of one trace analyzer, decide
/// independently.
/// </remarks>
public abstract class TraceRetention
{
    public static TraceRetention EveryObservation() => new EveryObservationRetention();
    public static TraceRetention EveryNth(int interval) => new EveryNthRetention(interval);
    public static TraceRetention OnChange(IEqualityComparer? equality = null) => new OnChangeRetention { Equality = equality };
    public static TraceRetention LatestOnly() => new LatestOnlyRetention();

    /// <summary>Creates the decider of one trace, which keeps whatever progress the retention needs.</summary>
    public abstract RetentionDecider<TValue> CreateDecider<TValue>();
}

/// <summary>The decisions of one trace, with the progress its retention keeps, such as a count or the previous value.</summary>
/// <remarks>The trace calls its decider under its own lock, one value at a time.</remarks>
public abstract class RetentionDecider<TValue>
{
    public abstract RetentionDecision Decide(TValue value);
}

public sealed class EveryObservationRetention : TraceRetention
{
    public override RetentionDecider<TValue> CreateDecider<TValue>() => new Decider<TValue>();

    private sealed class Decider<TValue> : RetentionDecider<TValue>
    {
        public override RetentionDecision Decide(TValue value) => RetentionDecision.Append;
    }
}

/// <summary>Records observations n, 2n, 3n, and so on. No extra initial or final entry is added.</summary>
public sealed class EveryNthRetention : TraceRetention
{
    public EveryNthRetention(int interval)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval);
        Interval = interval;
    }

    public int Interval { get; }

    public override RetentionDecider<TValue> CreateDecider<TValue>() => new Decider<TValue>(Interval);

    private sealed class Decider<TValue>(int interval) : RetentionDecider<TValue>
    {
        private long observed;

        public override RetentionDecision Decide(TValue value) =>
            ++observed % interval == 0 ? RetentionDecision.Append : RetentionDecision.Skip;
    }
}

/// <summary>
/// Records the first value and subsequent changes.
/// </summary>
/// <remarks>
/// Sameness is decided by <see cref="EqualityComparer{T}.Default"/> unless <see cref="Equality"/> holds a comparer.
/// That comparer is untyped because the retention serves traces of every value type.
/// </remarks>
public sealed class OnChangeRetention : TraceRetention
{
    /// <summary>Decides which observed values count as the same. Null uses the value type's default equality.</summary>
    public IEqualityComparer? Equality { get; init; }

    public override RetentionDecider<TValue> CreateDecider<TValue>() => new Decider<TValue>(Equality);

    private sealed class Decider<TValue>(IEqualityComparer? equality) : RetentionDecider<TValue>
    {
        private bool hasPrevious;
        private TValue? previous;

        public override RetentionDecision Decide(TValue value)
        {
            if (hasPrevious && AreSame(previous!, value))
                return RetentionDecision.Skip;
            previous = value;
            hasPrevious = true;
            return RetentionDecision.Append;
        }

        private bool AreSame(TValue left, TValue right) =>
            equality is null ? EqualityComparer<TValue>.Default.Equals(left, right) : equality.Equals(left, right);
    }
}

/// <summary>
/// Keeps one entry, the most recent, with the moment of its own observation. Earlier values are not history the
/// trace ever held, so a trace retaining this way answers what it reads now rather than how that moved.
/// </summary>
public sealed class LatestOnlyRetention : TraceRetention
{
    public override RetentionDecider<TValue> CreateDecider<TValue>() => new Decider<TValue>();

    private sealed class Decider<TValue> : RetentionDecider<TValue>
    {
        public override RetentionDecision Decide(TValue value) => RetentionDecision.ReplaceLatest;
    }
}
