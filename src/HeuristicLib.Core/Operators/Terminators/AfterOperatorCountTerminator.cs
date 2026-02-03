using HEAL.HeuristicLib.Operators.MetaOperators;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterOperatorCountTerminator<TGenotype>(InvocationCounter counter, int maximumCount /*, bool preventOverBudget = false*/)
  : Terminator<TGenotype>
  where TGenotype : class
{
  public InvocationCounter Counter { get; } = counter;
  public int MaximumCount { get; } = maximumCount;
  //public bool PreventOverBudget { get; } = preventOverBudget;

  public override Func<bool> CreateShouldTerminatePredicate() => () => Counter.CurrentCount >= MaximumCount;
}

// After evaluations terminator is not here because it actually a counting operator terminator wrapper. I am not sure if this is good.
