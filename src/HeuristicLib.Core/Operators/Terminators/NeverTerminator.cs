namespace HEAL.HeuristicLib.Operators.Terminators;

public class NeverTerminator<TGenotype>
  : StatelessTerminator<TGenotype>
  where TGenotype : class
{
  public override bool ShouldTerminate()
  {
    return false;
  }
}
