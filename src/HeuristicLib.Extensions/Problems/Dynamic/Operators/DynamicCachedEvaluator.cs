using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public class DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : CachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TSearchSpace>
  where TKey : notnull
  where TGenotype : class
{
  private int hitCount;

  public DynamicCachedEvaluator(
    Func<TGenotype, TKey> keySelector,
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    TProblem problem)
    : base(keySelector, evaluator)
  {
    problem.EpochClock.OnEpochChange += (_, _) => {
      ClearCache();
      hitCount = 0;
    };
  }
  public int GraceCount { get; init; } = int.MaxValue;

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TSearchSpace encoding,
    TProblem problem)
  {
    var (results, uniqueEvaluatedCount, cachedSolutionsCount)
      = EvaluateWithCache(solutions, random, encoding, problem);

    if (solutions.Count == 0) {
      return results;
    }

    if (uniqueEvaluatedCount == 0) {
      // Everything was cached in this batch
      hitCount += cachedSolutionsCount;
      if (hitCount >= GraceCount) {
        problem.EpochClock.AdvanceEpoch();
      }
    } else {
      // We had to actually evaluate something – reset streak
      hitCount = 0;
    }

    return results;
  }
}

public static class DynamicCachedEvaluatorExtension
{
  public static DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
    WithCache<TGenotype, TSearchSpace, TProblem, TKey>(this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
      TProblem problem,
      Func<TGenotype, TKey> keySelector)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TKey : notnull
    where TGenotype : class =>
    new(keySelector, evaluator, problem);

  public static DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>
    WithCache<TGenotype, TSearchSpace, TProblem>(this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
      TProblem problem)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class =>
    new(keySelector: x => x, evaluator, problem);

  public static DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
    GetCachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>(this TProblem problem, Func<TGenotype, TKey> keySelector)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class
    where TKey : notnull =>
    new(keySelector, new DirectEvaluator<TGenotype>(), problem);

  public static DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>
    GetCachedEvaluator<TGenotype, TSearchSpace, TProblem>(this TProblem problem)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class =>
    new(keySelector: x => x, new DirectEvaluator<TGenotype>(), problem);
}
