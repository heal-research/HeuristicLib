namespace HEAL.HeuristicLib.Operators.Terminators;

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}
