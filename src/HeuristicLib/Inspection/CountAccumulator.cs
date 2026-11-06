using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

public sealed class CountAccumulator : IRunResult
{
    private long currentCount;

    public long CurrentCount => Interlocked.Read(ref currentCount);

    public void IncrementBy(long by)
    {
        Interlocked.Add(ref currentCount, by);
    }
}
