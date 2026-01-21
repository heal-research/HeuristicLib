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

public class LimitingEvaluatorRewriter<TBuildSpec, TG, TS, TP>(int maxEvaluations, bool preventOverBudget)
  : IAlgorithmBuilderRewriter<TBuildSpec>
  where TBuildSpec : class, ISpecWithEvaluator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void Rewrite(TBuildSpec buildSpec) {
    //var countedEvaluator = buildSpec.Evaluator as CountedEvaluator<TG, TS, TP> ?? new CountedEvaluator<TG, TS, TP>(buildSpec.Evaluator);
    var countedEvaluator = buildSpec.Evaluator.AsCountedEvaluator();
    var limitEvaluator = new LimitEvaluator<TG, TS, TP>(countedEvaluator, maxEvaluations, preventOverBudget);
    buildSpec.Evaluator = limitEvaluator;
  }
}
