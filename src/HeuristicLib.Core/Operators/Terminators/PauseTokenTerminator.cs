namespace HEAL.HeuristicLib.Operators.Terminators;

public record class PauseTokenTerminator<TGenotype> : StatelessTerminator<TGenotype>
{
  private readonly PauseToken pauseToken;
  public PauseTokenTerminator(PauseToken pauseToken)
  {
    this.pauseToken = pauseToken;
  }

  public override bool ShouldTerminate()
  {
    return pauseToken.IsPaused;
  }
}
