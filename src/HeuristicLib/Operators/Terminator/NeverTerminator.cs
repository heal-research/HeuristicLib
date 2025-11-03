namespace HEAL.HeuristicLib.Operators;

public class NeverTerminator<TGenotype> : Terminator<TGenotype> {
  public override bool ShouldTerminate() {
    return false;
  }
}
