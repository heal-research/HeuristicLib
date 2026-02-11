namespace HEAL.HeuristicLib.Operators.Terminators;

public record NeverTerminator<TGenotype>
  : StatelessTerminator<TGenotype>
{
  public override bool ShouldTerminate()
  {
    return false;
  }
}
