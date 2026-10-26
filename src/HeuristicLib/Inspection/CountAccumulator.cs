namespace HEAL.HeuristicLib.Inspection;

public sealed class CountAccumulator
{
    private long currentCount;

    public long CurrentCount => Interlocked.Read(ref currentCount);

    public void IncrementBy(long by)
    {
        Interlocked.Add(ref currentCount, by);
    }
}
