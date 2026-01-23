namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IEpochClock {
  public int EpochCount { get; }
  public int EpochLength { get; }
  public int CurrentEpoch => EpochCount / EpochLength;

  public event EventHandler<int>? OnEpochChange;
  public EvaluationTiming IncreaseCount();
}
