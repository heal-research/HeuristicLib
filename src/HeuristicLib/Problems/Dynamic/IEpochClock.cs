namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IEpochClock {
  public long Ticks { get; }
  public int EpochLength { get; }
  public int CurrentEpoch => (int)(Ticks / EpochLength);

  public event EventHandler<int>? OnEpochChange;
  public EvaluationTiming IncreaseCount();
}
