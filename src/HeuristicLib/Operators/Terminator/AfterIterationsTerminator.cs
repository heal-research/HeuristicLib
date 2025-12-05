using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Prototypes;
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

public class TargetTerminator<TGenotype>(ObjectiveVector target) : Terminator<TGenotype, PopulationIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public override bool ShouldTerminate(PopulationIterationResult<TGenotype> currentIterationState,
                                       PopulationIterationResult<TGenotype>? previousIterationState,
                                       IEncoding<TGenotype> encoding,
                                       IProblem<TGenotype, IEncoding<TGenotype>> problem) =>
    currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
}

public static class TerminationExtensions {
  extension<TG, TE, TP, TR>(IAlgorithmBuilder<TG, TE, TP, TR> builder)
    where TG : class
    where TE : class, IEncoding<TG>
    where TP : class, IProblem<TG, TE>
    where TR : class, IIterationResult {
    public void SetMaxEvaluations(int maxEvaluations, bool preventOverBudget = false) {
      builder.AddAttachment(new AfterEvaluationsTermination<TG>(maxEvaluations, preventOverBudget));
    }

    public void SetMaxIterations(int maxIteration) {
      builder.Terminator = new AnyTerminator<TG, TR, TE, TP>(builder.Terminator, new AfterIterationsTerminator<TG>(maxIteration));
    }
  }

  extension<TG, TE, TP, TR>(IAlgorithmBuilder<TG, TE, TP, TR> builder)
    where TG : class
    where TE : class, IEncoding<TG>
    where TP : class, IProblem<TG, TE>
    where TR : PopulationIterationResult<TG> {
    public void SetTargetObjective(ObjectiveVector target) {
      builder.Terminator = new AnyTerminator<TG, TR, TE, TP>(builder.Terminator, new TargetTerminator<TG>(target));
    }
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
    : Terminator<T1, TRes1, TE1, TP1> where TRes1 : IIterationResult
    where TE1 : class, IEncoding<T1>
    where TP1 : class, IProblem<T1, TE1> {
    public override bool ShouldTerminate(TRes1 currentIterationState, TRes1? previousIterationState, TE1 encoding, TP1 problem) =>
      termination.CurrentCount >= termination.MaximumEvaluations;
  }

  public class EvaluationLimitEvaluator<T1, TE1, TP1>(AfterEvaluationsTermination<TGenotype> termination, IEvaluator<T1, TE1, TP1> evaluator)
    : BatchEvaluator<T1, TE1, TP1> where TE1 : class, IEncoding<T1> where TP1 : class, IProblem<T1, TE1> {
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> solutions, IRandomNumberGenerator random, TE1 encoding, TP1 problem) {
      int count = solutions.Count;
      int n = count;

      if (termination.PreventOverBudget) {
        int remaining = termination.MaximumEvaluations - termination.CurrentCount;
        if (remaining <= 0) return Enumerable.Repeat(problem.Objective.Worst, count).ToArray();
        if (n > remaining) n = remaining;
      }

      termination.CurrentCount += n;
      if (n == count)
        return evaluator.Evaluate(solutions, random, encoding, problem);
      return evaluator.Evaluate(solutions.Take(n).ToList(), random, encoding, problem)
                      .Concat(Enumerable.Repeat(problem.Objective.Worst, count - n))
                      .ToArray();
    }
  }
}
