using HEAL.HeuristicLib.Analysis;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterOperatorCountTerminator<TGenotype> : StatelessTerminator<TGenotype>
{
  public AfterOperatorCountTerminator(InvocationCounter counter, int maximumCount)
  {
    this.counter = counter;
    this.maximumCount = maximumCount;
  }
  private readonly InvocationCounter counter;
  private readonly int maximumCount;
  
  public override bool ShouldTerminate()
  {
    return counter.CurrentCount >= maximumCount;
  }
}

// ToDo: add extensions for common counter hooks, e.g. after evaluations count terminator.
