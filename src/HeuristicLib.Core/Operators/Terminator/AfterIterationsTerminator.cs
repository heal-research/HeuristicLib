using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Terminator;

public class AfterIterationsTerminator<TGenotype>(int maximumIterations) : Terminator<TGenotype> {
  public int MaximumIterations { get; } = maximumIterations;

  public int CurrentCount { get; private set; }

  public override bool ShouldTerminate() {
    return ++CurrentCount >= MaximumIterations;
  }
}

public class AfterEvaluationsTermination<TGenotype>(int maximumEvaluations, bool preventOverBudget) : AlgorithmAttachment<TGenotype> where TGenotype : class {
  public int MaximumEvaluations { get; } = maximumEvaluations;
  public int CurrentCount { get; private set; }

  public bool PreventOverBudget { get; } = preventOverBudget;

  public override IEvaluator<T1, TE1, TP1> WrapEvaluator<T1, TE1, TP1>(IEvaluator<T1, TE1, TP1> evaluator) =>
    new EvaluationLimitEvaluator<T1, TE1, TP1>(this, evaluator);

  public override ITerminator<T1, TRes1, TE1, TP1> WrapTerminator<T1, TRes1, TE1, TP1>(ITerminator<T1, TRes1, TE1, TP1> terminator) =>
    new AnyTerminator<T1, TRes1, TE1, TP1>(terminator, new AfterEvaluationsTerminator<T1, TRes1, TE1, TP1>(this));

  public class AfterEvaluationsTerminator<T1, TRes1, TE1, TP1>(AfterEvaluationsTermination<TGenotype> termination)
    : Terminator<T1, TRes1, TE1, TP1> where TRes1 : IAlgorithmState
    where TE1 : class, IEncoding<T1>
    where TP1 : class, IProblem<T1, TE1> {
    public override bool ShouldTerminate(TRes1 currentIterationState, TRes1? previousIterationState, TE1 encoding, TP1 problem) =>
      termination.CurrentCount >= termination.MaximumEvaluations;
  }

  public class EvaluationLimitEvaluator<T1, TE1, TP1>(AfterEvaluationsTermination<TGenotype> termination, IEvaluator<T1, TE1, TP1> evaluator)
    : BatchEvaluator<T1, TE1, TP1> where TE1 : class, IEncoding<T1> where TP1 : class, IProblem<T1, TE1> {
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> genotypes, IRandomNumberGenerator random, TE1 encoding, TP1 problem) {
      var count = genotypes.Count;
      var n = count;

      if (termination.PreventOverBudget) {
        var remaining = termination.MaximumEvaluations - termination.CurrentCount;
        if (remaining <= 0) return Enumerable.Repeat(problem.Objective.Worst, count).ToArray();
        if (n > remaining) n = remaining;
      }

      termination.CurrentCount += n;
      if (n == count)
        return evaluator.Evaluate(genotypes, random, encoding, problem);
      return evaluator.Evaluate(genotypes.Take(n).ToList(), random, encoding, problem)
                      .Concat(Enumerable.Repeat(problem.Objective.Worst, count - n))
                      .ToArray();
    }
  }
}
