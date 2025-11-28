using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
  : CachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TEncoding>
  where TKey : notnull
  where TGenotype : class {
  public int GraceCount { get; init; } = int.MaxValue;
  private int hitCount = 0;

  public DynamicCachedEvaluator(
    Func<TGenotype, TKey> keySelector,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    TProblem problem)
    : base(keySelector, evaluator) {
    problem.EpochClock.OnEpochChange += (_, _) => ClearCache();
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TEncoding encoding,
    TProblem problem) {
    var (results, uniqueEvaluatedCount, cachedSolutionsCount)
      = EvaluateWithCache(solutions, random, encoding, problem);

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
