namespace HEAL.HeuristicLib.Operators.Terminator;

public class AfterIterationsTerminator<TGenotype>(int maximumIterations) : Terminator<TGenotype> {
  public int MaximumIterations { get; } = maximumIterations;

  public int CurrentCount { get; private set; } = 0;

  public override bool ShouldTerminate() {
    CurrentCount += 1;
    return CurrentCount >= MaximumIterations;
  }
}
