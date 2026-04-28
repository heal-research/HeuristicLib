namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterIterationsTerminator<TGenotype>
  : Terminator<TGenotype, AfterIterationsTerminator<TGenotype>.ExecutionState>
{
  public sealed class ExecutionState
  {
    public int CurrentCounter { get; set; }
  }

  private readonly int maximumIterations;

  public AfterIterationsTerminator(int maximumIterations)
  {
    this.maximumIterations = maximumIterations;
  }

  protected override ExecutionState CreateInitialState() => new();

  protected override bool ShouldTerminate(ExecutionState executionState)
  {
    executionState.CurrentCounter += 1;
    return executionState.CurrentCounter >= maximumIterations;
  }
}
