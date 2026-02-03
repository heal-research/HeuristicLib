namespace HEAL.HeuristicLib.Operators.Terminator;

public class NeverTerminator<TGenotype> : Terminator<TGenotype> {
  public override bool ShouldTerminate() {
    return false;
  }
}
