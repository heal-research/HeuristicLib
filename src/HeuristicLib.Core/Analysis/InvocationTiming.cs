namespace HEAL.HeuristicLib.Analysis;

public sealed class InvocationTiming
{
  private TimeSpan totalTime = TimeSpan.Zero;
  
  public TimeSpan TotalTime => totalTime;
  
  public void AddTime(TimeSpan time)
  {
    totalTime += time;
  }
}

