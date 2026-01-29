namespace HEAL.HeuristicLib.Operators.Terminators;

public class NeverTerminator<TGenotype> : Terminator<TGenotype>
{
  public override Func<bool> CreateShouldTerminatePredicate() => () => false;
}
