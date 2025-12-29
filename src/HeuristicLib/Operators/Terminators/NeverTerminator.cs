namespace HEAL.HeuristicLib.Operators.Terminators;

public class NeverTerminator<TGenotype> : Terminator<TGenotype> {
  public override bool ShouldTerminate() {
    return false;
  }
}
