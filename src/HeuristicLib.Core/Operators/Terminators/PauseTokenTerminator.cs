namespace HEAL.HeuristicLib.Operators.Terminators;

public class PauseTokenTerminator<TGenotype>(PauseToken pauseToken) : Terminator<TGenotype>
{
  public override Func<bool> CreateShouldTerminatePredicate()
  {
    return () => pauseToken.IsPaused;
  }
}
