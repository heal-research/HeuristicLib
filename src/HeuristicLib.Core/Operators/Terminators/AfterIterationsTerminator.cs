namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterIterationsTerminator<TGenotype>
  : StatefulTerminator<TGenotype, AfterIterationsTerminator<TGenotype>.State>
{
  public sealed class State
  {
    public int CurrentCounter { get; set; }
  }

  private readonly int maximumIterations;

  public AfterIterationsTerminator(int maximumIterations)
  {
    this.maximumIterations = maximumIterations;
  }

  protected override State CreateInitialState() => new();

  protected override bool ShouldTerminate(State terminatorState)
  {
    terminatorState.CurrentCounter += 1;
    return terminatorState.CurrentCounter >= maximumIterations;
  }
}
