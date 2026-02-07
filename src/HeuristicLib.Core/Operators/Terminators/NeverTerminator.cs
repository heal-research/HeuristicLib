namespace HEAL.HeuristicLib.Operators.Terminators;

public class NeverTerminator<TGenotype>
  : StatelessTerminator<TGenotype>
{
  public override bool ShouldTerminate()
  {
    return false;
  }
}
