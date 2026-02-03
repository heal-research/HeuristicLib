using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public class DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
  : CachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TEncoding>
  where TKey : notnull
  where TGenotype : class {
  public int GraceCount { get; init; } = int.MaxValue;
  private int hitCount;

  public DynamicCachedEvaluator(
    Func<TGenotype, TKey> keySelector,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    TProblem problem)
    : base(keySelector, evaluator) {
    problem.EpochClock.OnEpochChange += (_, _) => {
      ClearCache();
      hitCount = 0;
    };
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

public static class DynamicCachedEvaluatorExtension {
  public static DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
    WithCache<TGenotype, TEncoding, TProblem, TKey>(this IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                                                    TProblem problem,
                                                    Func<TGenotype, TKey> keySelector)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TEncoding>
    where TKey : notnull
    where TGenotype : class {
    return new DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>(keySelector, evaluator, problem);
  }

  public static DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TGenotype>
    WithCache<TGenotype, TEncoding, TProblem>(this IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                                              TProblem problem)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TEncoding>
    where TGenotype : class {
    return new DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TGenotype>(x => x, evaluator, problem);
  }

  public static DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
    GetCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>(this TProblem problem, Func<TGenotype, TKey> keySelector)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TEncoding>
    where TGenotype : class
    where TKey : notnull {
    return new DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey>(keySelector, new DirectEvaluator<TGenotype>(), problem);
  }

  public static DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TGenotype>
    GetCachedEvaluator<TGenotype, TEncoding, TProblem>(this TProblem problem)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TEncoding>
    where TGenotype : class {
    return new DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TGenotype>(x => x, new DirectEvaluator<TGenotype>(), problem);
  }
}
