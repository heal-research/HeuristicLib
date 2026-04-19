namespace HEAL.HeuristicLib.Operators.Terminators;

public record NeverTerminator<TGenotype>
  : StatelessTerminator<TGenotype>
{
  public override bool ShouldTerminate() => NeverTerminator.ShouldTerminate();
}

public static class NeverTerminator
{
  public static bool ShouldTerminate() => false;
}
