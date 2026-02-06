namespace HEAL.HeuristicLib.Analysis;

public sealed class InvocationCounter
{
  private int currentCount = 0;

  public int CurrentCount => currentCount;
  
  public void IncrementBy(int by)
  {
    Interlocked.Add(ref currentCount, by);
  }
}
