using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterIterationsTerminator<TGenotype>
  : Terminator<TGenotype>
{
  private readonly int maximumIterations;

  public AfterIterationsTerminator(int maximumIterations)
  {
    this.maximumIterations = maximumIterations;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new Instance(maximumIterations);
  }

  public class Instance : TerminatorInstance<TGenotype>
  {
    private readonly int maximumIterations;
    private int currentCounter;

    public Instance(int maximumIterations)
    {
      this.maximumIterations = maximumIterations;
      currentCounter = 0;
    }


    public override bool ShouldTerminate()
    {
      return currentCounter++ >= maximumIterations;
    }
  }
}
