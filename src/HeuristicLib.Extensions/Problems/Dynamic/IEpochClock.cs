namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IEpochClock
{
  long Ticks { get; }
  int EpochLength { get; }
  int CurrentEpoch => (int)(Ticks / EpochLength);

  event EventHandler<int>? OnEpochChange;
  EvaluationTiming IncreaseCount();
}
