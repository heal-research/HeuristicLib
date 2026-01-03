using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

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

public class LimitingEvaluatorRewriter<TBuilder, TG, TS, TP, TR, TA>(int maxEvaluations, bool preventOverBudget)
  : IAlgorithmBuilderRewriter<TBuilder, TG, TS, TP, TR, TA>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TA : class, IAlgorithm<TG, TS, TP, TR> 
  where TBuilder : IAlgorithmBuilder<TG, TS, TP, TR, TA>, IBuilderWithEvaluator<TG, TS, TP, TR, TA>
{
  public void Rewrite(TBuilder builder) {
    var countedEvaluator = builder.Evaluator as CountedEvaluator<TG, TS, TP> ?? new CountedEvaluator<TG, TS, TP>(builder.Evaluator);
    var limitEvaluator = new LimitEvaluator<TG, TS, TP>(countedEvaluator, maxEvaluations, preventOverBudget);
    builder.Evaluator = limitEvaluator;
  }
}
