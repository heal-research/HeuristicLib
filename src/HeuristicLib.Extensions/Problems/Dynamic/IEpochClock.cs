namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IEpochClock
{
  int EpochCount { get; }
  int EpochLength { get; }
  int CurrentEpoch => EpochCount / EpochLength;

  event EventHandler<int>? OnEpochChange;
  EvaluationTiming IncreaseCount();
}
