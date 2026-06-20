namespace HEAL.HeuristicLib.Analysis;

public sealed class ObservationCounter
{
    private int currentCount;

    public int CurrentCount => currentCount;

    public void IncrementBy(int by)
    {
        Interlocked.Add(ref currentCount, by);
    }
}
