namespace HEAL.HeuristicLib.Instrumentation;

public sealed class CountAccumulator
{
    private int currentCount;

    public int CurrentCount => currentCount;

    public void IncrementBy(int by)
    {
        Interlocked.Add(ref currentCount, by);
    }
}
