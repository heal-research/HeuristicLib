using System.Collections;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>What a trace does with one computed value.</summary>
public enum RetentionDecision
{
    /// <summary>Store the value as a new entry. This is the value an unset decision has.</summary>
    Append,

    /// <summary>Replace the latest entry, or append the first entry when none exists.</summary>
    ReplaceLatest,

    /// <summary>Drop the value. The entries already stored are untouched.</summary>
    Skip
}

/// <summary>
/// Decides which computed values a trace retains.
/// </summary>
/// <remarks>
/// A retention is an immutable policy. What it counts or remembers belongs to the <see cref="RetentionDecider{TValue}"/>
/// it creates for each trace, so traces sharing one retention, and the runs of one trace analyzer, decide
/// independently. The value function runs before retention, including for skipped values. A best-so-far curve
/// applies retention to the running best candidate, rather than to the whole point.
/// </remarks>
public abstract class TraceRetention
{
    public static TraceRetention EveryObservation() => new EveryObservationRetention();
    /// <summary>Retains values at observations n, 2n, 3n, and so on, without extra initial or final entries.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not positive.</exception>
    public static TraceRetention EveryNth(int interval) => new EveryNthRetention(interval);
    /// <summary>Retains the first value and subsequent changes under the given equality.</summary>
    /// <param name="equality">Compares values; <see langword="null"/> uses the value type's default equality.</param>
    public static TraceRetention OnChange(IEqualityComparer? equality = null) => new OnChangeRetention { Equality = equality };
    /// <summary>Retains only the most recent value and its own clock readings.</summary>
    public static TraceRetention LatestOnly() => new LatestOnlyRetention();

    /// <summary>Creates the decider of one trace, which keeps whatever progress the retention needs.</summary>
    public abstract RetentionDecider<TValue> CreateDecider<TValue>();
}

/// <summary>The decisions of one trace, with the progress its retention keeps, such as a count or the previous value.</summary>
/// <remarks>The trace calls its decider under its own lock, one value at a time.</remarks>
public abstract class RetentionDecider<TValue>
{
    /// <summary>Decides how to retain the value, updating this result's retention progress.</summary>
    /// <remarks>Must return a defined <see cref="RetentionDecision"/>; an unknown value fails collection.</remarks>
    public abstract RetentionDecision Decide(TValue value);
}

/// <summary>Appends every computed value without keeping retention progress.</summary>
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not positive.</exception>
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
