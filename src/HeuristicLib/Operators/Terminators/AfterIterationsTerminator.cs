using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterIterationsTerminator<TGenotype>(int maximumIterations) : Terminator<TGenotype> {
  public int MaximumIterations { get; } = maximumIterations;

  public int CurrentCount { get; private set; }

  public override bool ShouldTerminate() {
    return ++CurrentCount >= MaximumIterations;
  }
}

public class TargetTerminator<TGenotype>(ObjectiveVector target) : Terminator<TGenotype, PopulationIterationResult<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> {
  public override bool ShouldTerminate(PopulationIterationResult<TGenotype> currentIterationState,
                                       PopulationIterationResult<TGenotype>? previousIterationState,
                                       ISearchSpace<TGenotype> searchSpace,
                                       IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
    currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
}

public static class TerminationExtensions {
  extension<TG, TS, TP, TR>(IAlgorithmBuilder<TG, TS, TP, TR> builder)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IIterationResult {
    public void SetMaxEvaluations(int maxEvaluations, bool preventOverBudget = false) {
      builder.AddAttachment(new AfterEvaluationsTermination<TG>(maxEvaluations, preventOverBudget));
    }

    public void SetMaxIterations(int maxIteration) {
      builder.Terminator = new AnyTerminator<TG, TR, TS, TP>(builder.Terminator, new AfterIterationsTerminator<TG>(maxIteration));
    }
  }

  extension<TG, TS, TP, TR>(IAlgorithmBuilder<TG, TS, TP, TR> builder)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : PopulationIterationResult<TG> {
    public void SetTargetObjective(ObjectiveVector target) {
      builder.Terminator = new AnyTerminator<TG, TR, TS, TP>(builder.Terminator, new TargetTerminator<TG>(target));
    }
  }
}

public class AfterEvaluationsTermination<TGenotype>(int maximumEvaluations, bool preventOverBudget) : AlgorithmAttachment<TGenotype> where TGenotype : class {
  public int MaximumEvaluations { get; } = maximumEvaluations;
  public int CurrentCount { get; private set; }

  public bool PreventOverBudget { get; } = preventOverBudget;

  public override IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1>(IEvaluator<T1, TS1, TP1> evaluator) =>
    new EvaluationLimitEvaluator<T1, TS1, TP1>(this, evaluator);

  public override ITerminator<T1, TRes1, TS1, TP1> WrapTerminator<T1, TRes1, TS1, TP1>(ITerminator<T1, TRes1, TS1, TP1> terminator) =>
    new AnyTerminator<T1, TRes1, TS1, TP1>(terminator, new AfterEvaluationsTerminator<T1, TRes1, TS1, TP1>(this));

  public class AfterEvaluationsTerminator<T1, TRes1, TS1, TP1>(AfterEvaluationsTermination<TGenotype> termination)
    : Terminator<T1, TRes1, TS1, TP1> where TRes1 : IIterationResult
    where TS1 : class, ISearchSpace<T1>
    where TP1 : class, IProblem<T1, TS1> {
    public override bool ShouldTerminate(TRes1 currentIterationState, TRes1? previousIterationState, TS1 searchSpace, TP1 problem) =>
      termination.CurrentCount >= termination.MaximumEvaluations;
  }

  public class EvaluationLimitEvaluator<T1, TS1, TP1>(AfterEvaluationsTermination<TGenotype> termination, IEvaluator<T1, TS1, TP1> evaluator)
    : BatchEvaluator<T1, TS1, TP1> where TS1 : class, ISearchSpace<T1> where TP1 : class, IProblem<T1, TS1> {
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> solutions, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      int count = solutions.Count;
      int n = count;

      if (termination.PreventOverBudget) {
        int remaining = termination.MaximumEvaluations - termination.CurrentCount;
        if (remaining <= 0) return Enumerable.Repeat(problem.Objective.Worst, count).ToArray();
        if (n > remaining) n = remaining;
      }

      termination.CurrentCount += n;
      if (n == count)
        return evaluator.Evaluate(solutions, random, searchSpace, problem);
      return evaluator.Evaluate(solutions.Take(n).ToList(), random, searchSpace, problem)
                      .Concat(Enumerable.Repeat(problem.Objective.Worst, count - n))
                      .ToArray();
    }
  }
}
