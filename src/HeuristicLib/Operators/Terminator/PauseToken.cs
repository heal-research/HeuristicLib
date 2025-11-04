namespace HEAL.HeuristicLib.Operators;

public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}
