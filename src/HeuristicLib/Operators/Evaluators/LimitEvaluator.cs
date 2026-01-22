using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class LimitEvaluator<TG, TS, TP>(CountedEvaluator<TG, TS, TP> evaluator, int maxEvaluations, bool preventOverBudget) : Evaluator<TG, TS, TP> 
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public LimitEvaluator(IEvaluator<TG, TS, TP> evaluator, int maxEvaluations, bool preventOverBudget)
    : this(evaluator.AsCountedEvaluator(), maxEvaluations, preventOverBudget) { }
  
  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> solutions, IRandomNumberGenerator random, TS searchSpace, TP problem) {
    int count = solutions.Count;
    int n = count;
    
    if (preventOverBudget) {
      int remaining = maxEvaluations - evaluator.CurrentCount;
      if (remaining <= 0) return Enumerable.Repeat(problem.Objective.Worst, count).ToArray();
      if (n > remaining) n = remaining;
    }

    if (n == count) {
      return evaluator.Evaluate(solutions, random, searchSpace, problem);
    } else {
      var evaluated = evaluator.Evaluate(solutions.Take(n).ToList(), random, searchSpace, problem);
      var fill = Enumerable.Repeat(problem.Objective.Worst, count - n);
      return evaluated.Concat(fill).ToArray();
    }
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
    var countedEvaluator = buildSpec.Evaluator.AsCountedEvaluator();
    // ToDo: think about only using the limit evaluator if prevenOverBudget is true
    var limitEvaluator = new LimitEvaluator<TG, TS, TP>(countedEvaluator, maxEvaluations, preventOverBudget);
    buildSpec.Evaluator = limitEvaluator;
  }
}
