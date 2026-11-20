using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A mutable count with atomic reads and additions, initially zero.</summary>
public sealed class CountAccumulator : IRunResult
{
    private long currentCount;

    public long CurrentCount => Interlocked.Read(ref currentCount);

    /// <summary>Atomically adds the signed amount, without overflow checking.</summary>
    public void IncrementBy(long by)
    {
        Interlocked.Add(ref currentCount, by);
    }
}
