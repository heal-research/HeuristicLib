namespace HEAL.HeuristicLib.Core;

public interface IBuilder<out TResult> {
  TResult Build();
}
