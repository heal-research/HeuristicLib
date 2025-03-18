namespace HEAL.HeuristicLib.Algorithms;

public interface IBuilder<out TResult> {
  TResult Build();
}
