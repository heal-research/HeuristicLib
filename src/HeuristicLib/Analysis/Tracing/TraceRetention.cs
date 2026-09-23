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
/// A retention is an object rather than a setting. One that counts or remembers owns that state, so two traces
/// sharing one object share its counting. The factories here return a fresh retention per call, which is what makes
/// the ordinary inline use independent.
/// </remarks>
public abstract class TraceRetention
{
    public static TraceRetention EveryObservation() => new EveryObservationRetention();
    public static TraceRetention EveryNth(int interval) => new EveryNthRetention(interval);
    public static TraceRetention OnChange(IEqualityComparer? equality = null) => new OnChangeRetention { Equality = equality };
    public static TraceRetention LatestOnly() => new LatestOnlyRetention();

    public abstract RetentionDecision Decide<T>(T value);
}

public sealed class EveryObservationRetention : TraceRetention
{
    public override RetentionDecision Decide<T>(T value) => RetentionDecision.Append;
}

/// <summary>Records observations n, 2n, 3n, and so on. No extra initial or final entry is added.</summary>
public sealed class EveryNthRetention : TraceRetention
{
    private long observed;

    public EveryNthRetention(int interval)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval);
        Interval = interval;
    }

    public int Interval { get; }

    public override RetentionDecision Decide<T>(T value) =>
        Interlocked.Increment(ref observed) % Interval == 0 ? RetentionDecision.Append : RetentionDecision.Skip;
}

/// <summary>
/// Records the first value and subsequent changes.
/// </summary>
/// <remarks>
/// Sameness is decided by <see cref="EqualityComparer{T}.Default"/> unless <see cref="Equality"/> holds a comparer.
/// That comparer is untyped because the observed value's type is not known until an observation arrives.
/// </remarks>
public sealed class OnChangeRetention : TraceRetention
{
    private readonly Lock sync = new();
    private object? previous;
    private Type? previousType;

    /// <summary>Decides which observed values count as the same. Null uses the value type's default equality.</summary>
    public IEqualityComparer? Equality { get; init; }

    public override RetentionDecision Decide<T>(T value)
    {
        lock (sync)
        {
            if (previousType == typeof(T) && AreSame((T)previous!, value))
                return RetentionDecision.Skip;
            previous = value;
            previousType = typeof(T);
            return RetentionDecision.Append;
        }
    }

    private bool AreSame<T>(T left, T right) =>
        Equality is null ? EqualityComparer<T>.Default.Equals(left, right) : Equality.Equals(left, right);
}

/// <summary>
/// Keeps one entry, the most recent, with the moment of its own observation. Earlier values are not history the
/// trace ever held, so a trace retaining this way answers what its measurement says now rather than how it moved.
/// </summary>
public sealed class LatestOnlyRetention : TraceRetention
{
    public override RetentionDecision Decide<T>(T value) => RetentionDecision.ReplaceLatest;
}
