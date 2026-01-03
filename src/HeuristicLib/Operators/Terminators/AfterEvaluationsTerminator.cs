using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterEvaluationsTerminator<T1, TRes1, TS1, TP1>(AfterEvaluationsTerminator<T1> terminator)
  : Terminator<T1, TRes1, TS1, TP1> where TRes1 : IAlgorithmState
  where TS1 : class, ISearchSpace<T1>
  where TP1 : class, IProblem<T1, TS1>
  where T1 : class {
  public override bool ShouldTerminate(TRes1 currentIterationState, TRes1? previousIterationState, TS1 searchSpace, TP1 problem) =>
    terminator.CurrentCount >= terminator.MaximumEvaluations;
}

public class EvaluationLimitEvaluator<T1, TS1, TP1>(AfterEvaluationsTerminator<T1> terminator, IEvaluator<T1, TS1, TP1> evaluator)
  : Evaluator<T1, TS1, TP1> where TS1 : class, ISearchSpace<T1> where TP1 : class, IProblem<T1, TS1> where T1 : class {
  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> solutions, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
    int count = solutions.Count;
    int n = count;

    if (terminator.PreventOverBudget) {
      int remaining = terminator.MaximumEvaluations - terminator.CurrentCount;
      if (remaining <= 0) return Enumerable.Repeat(problem.Objective.Worst, count).ToArray();
      if (n > remaining) n = remaining;
    }

    //terminator.CurrentCount += n;
    if (n == count)
      return evaluator.Evaluate(solutions, random, searchSpace, problem);
    return evaluator.Evaluate(solutions.Take(n).ToList(), random, searchSpace, problem)
      .Concat(Enumerable.Repeat(problem.Objective.Worst, count - n))
      .ToArray();
  }
  
  //
  // public override IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1>(IEvaluator<T1, TS1, TP1> evaluator) =>
  //   new EvaluationLimitEvaluator<T1, TS1, TP1>(this, evaluator);
  //
  // public override ITerminator<T1, TRes1, TS1, TP1> WrapTerminator<T1, TRes1, TS1, TP1>(ITerminator<T1, TRes1, TS1, TP1> terminator) =>
  //   new AnyTerminator<T1, TRes1, TS1, TP1>(terminator, new AfterEvaluationsTerminator<T1, TRes1, TS1, TP1>(this));
}
