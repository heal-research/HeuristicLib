namespace HEAL.HeuristicLib.Analysis;

public sealed class ObservationDuration
{
    private long currentDurationTicks;

    public TimeSpan CurrentDuration => TimeSpan.FromTicks(Interlocked.Read(ref currentDurationTicks));

    public void AddDuration(TimeSpan duration)
    {
        Interlocked.Add(ref currentDurationTicks, duration.Ticks);
    }
}
