using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A mutable total duration with atomic reads and additions, initially zero.</summary>
public sealed class DurationAccumulator : IRunResult
{
    private long currentDurationTicks;

    public TimeSpan CurrentDuration => TimeSpan.FromTicks(Interlocked.Read(ref currentDurationTicks));

    /// <summary>Atomically adds the signed duration in ticks, without overflow checking.</summary>
    public void AddDuration(TimeSpan duration)
    {
        Interlocked.Add(ref currentDurationTicks, duration.Ticks);
    }
}
