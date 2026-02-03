namespace HEAL.HeuristicLib.Operators.Terminators;

public class PauseTokenTerminator<TGenotype>(PauseToken pauseToken) : Terminator<TGenotype>
{
  public override bool ShouldTerminate() => pauseToken.IsPaused;
}
