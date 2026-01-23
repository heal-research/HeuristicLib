using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public class DynamicCachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : CachedEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TSearchSpace>
  where TKey : notnull
  where TGenotype : class {
  public int GraceCount { get; init; } = int.MaxValue;
  private int hitCount = 0;

  public DynamicCachedEvaluator(
    Func<TGenotype, TKey> keySelector,
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    TProblem problem)
    : base(keySelector, evaluator) {
    problem.EpochClock.OnEpochChange += (_, _) => ClearCache();
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TSearchSpace searchSpace,
    TProblem problem) {
    var (results, uniqueEvaluatedCount, cachedSolutionsCount)
      = EvaluateWithCache(solutions, random, searchSpace, problem);

    if (solutions.Count == 0)
      return results;

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
