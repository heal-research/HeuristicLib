using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public interface ICountedOperator
{
  int CurrentCount { get; }
}

public class CountedEvaluator<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
  : Evaluator<TG, TS, TP>, ICountedOperator
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int CurrentCount { get; private set; }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> solutions, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    CurrentCount += solutions.Count;
    return evaluator.Evaluate(solutions, random, searchSpace, problem);
  }
}

public static class CountedEvaluatorExtensions
{
  extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public CountedEvaluator<TG, TS, TP> AsCountedEvaluator() => evaluator as CountedEvaluator<TG, TS, TP> ?? new CountedEvaluator<TG, TS, TP>(evaluator);
  }
}
