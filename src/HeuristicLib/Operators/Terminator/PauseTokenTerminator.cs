namespace HEAL.HeuristicLib.Operators;

public class PauseTokenTerminator<TGenotype>(PauseToken pauseToken) : Terminator<TGenotype> {
  public override bool ShouldTerminate() {
    return pauseToken.IsPaused;
  }
}
