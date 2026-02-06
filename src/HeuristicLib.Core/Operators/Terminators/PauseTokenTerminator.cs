namespace HEAL.HeuristicLib.Operators.Terminators;

public class PauseTokenTerminator<TGenotype> : StatelessTerminator<TGenotype>
  where TGenotype : class
{
  private readonly PauseToken pauseToken;
  public PauseTokenTerminator(PauseToken pauseToken) {
    this.pauseToken = pauseToken;
  }

  public override bool ShouldTerminate()
  {
    return pauseToken.IsPaused;
  }
}
