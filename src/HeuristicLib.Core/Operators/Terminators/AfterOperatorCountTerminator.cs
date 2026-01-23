using HEAL.HeuristicLib.Operators.Evaluators;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterOperatorCountTerminator<TGenotype>(ICountedOperator @operator, int maximumCount/*, bool preventOverBudget = false*/) 
  : Terminator<TGenotype> 
  where TGenotype : class 
{
  public ICountedOperator Operator { get; } = @operator;
  public int MaximumCount { get; } = maximumCount;
  //public bool PreventOverBudget { get; } = preventOverBudget;
  
  public override bool ShouldTerminate() {
    return Operator.CurrentCount >= MaximumCount;
  }
}

// After evaluations terminator is not here because it actually a counting operator terminator wrapper. I am not sure if this is good.
