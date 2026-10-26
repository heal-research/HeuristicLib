using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// One typed notion of time a trace can record its entries against.
/// </summary>
/// <remarks>
/// Derive from <see cref="Clock{TTime}"/> to add a clock of your own, such as one over a domain quantity the library
/// knows nothing about. A clock belongs beside the thing it reads and is offered as an extension on <see cref="Clock"/>
/// named for the time it reads, such as <c>Clock.FromCandidates</c>. A clock whose time is kept current by advice
/// assigns it in its constructor; traces apply a clock's advice before their own observations.
/// </remarks>
public abstract class Clock : Aspect
{
    private protected Clock() { }

    internal abstract object Read();

    /// <summary>
    /// Creates a clock measuring wall-clock time since its creation.
    /// </summary>
    public static ElapsedTimeClock FromElapsedTime(TimeProvider timeProvider) => new(timeProvider);
}

/// <summary>
/// A clock whose time has the type <typeparamref name="TTime"/>.
/// </summary>
/// <remarks>
/// This is the type a custom clock derives from. The clock object is also the key a trace is read back with, so one
/// instance means one axis: two clocks of the same kind stay distinguishable.
/// </remarks>
public abstract class Clock<TTime> : Clock
{
    protected Clock() { }

    /// <summary>
    /// Reads the time in effect now. Called once per recorded trace entry.
    /// </summary>
    protected abstract TTime ReadTime();

    internal sealed override object Read() => ReadTime()!;
}

/// <summary>
/// Measures wall-clock time since this clock was created.
/// </summary>
public sealed class ElapsedTimeClock(TimeProvider timeProvider) : Clock<TimeSpan>
{
    private readonly long startedAt = timeProvider.GetTimestamp();

    protected override TimeSpan ReadTime() => timeProvider.GetElapsedTime(startedAt);
}
