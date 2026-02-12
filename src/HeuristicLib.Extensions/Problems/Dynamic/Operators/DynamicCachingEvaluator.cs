using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : CachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TSearchSpace>
  where TGenotype : class
  where TKey : notnull
{
  private readonly TProblem problem;

  public DynamicCachingEvaluator(
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    TProblem problem,
    Func<TGenotype, TKey>? keySelector = null, long? sizeLimit = null)
    : base(evaluator, keySelector, sizeLimit)
  {
    this.problem = problem;
  }

  public long GraceCount { get; init; } = long.MaxValue;

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.Resolve(Evaluator);
    return new Instance(problem, evaluatorInstance, KeySelector, SizeLimit, GraceCount);
  }

  public new class Instance : CachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>.Instance
  {
    private readonly long graceCount;
    private long hitCount;

    public Instance(TProblem problem, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey> keySelector, long? sizeLimit, long graceCount)
      : base(evaluator, keySelector, sizeLimit)
    {
      this.graceCount = graceCount;

      problem.EpochClock.OnEpochChange += (_, _) => {
        ClearCache();
        hitCount = 0;
      };
    }

    public override IReadOnlyList<ObjectiveVector> Evaluate(
      IReadOnlyList<TGenotype> genotypes,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
    {
      var beforeCacheStatistics = Cache.GetCurrentStatistics();
      var beforeHits = beforeCacheStatistics?.TotalHits ?? 0;
      var beforeMisses = beforeCacheStatistics?.TotalMisses ?? 0;

      var results = base.Evaluate(genotypes, random, searchSpace, problem);

      var afterCacheStatistics = Cache.GetCurrentStatistics();
      var afterHits = afterCacheStatistics?.TotalHits ?? 0;
      var afterMisses = afterCacheStatistics?.TotalMisses ?? 0;

      var uniqueEvaluatedCount = afterMisses - beforeMisses;
      var cachedSolutionsCount = afterHits - beforeHits;

      if (genotypes.Count == 0) {
        return results;
      }

      if (uniqueEvaluatedCount == 0) {
        // Everything was cached in this batch
        hitCount += cachedSolutionsCount;
        if (hitCount >= graceCount) {
          problem.EpochClock.AdvanceEpoch();
        }
      } else {
        // We had to actually evaluate something – reset streak
        hitCount = 0;
      }

      return results;
    }

    private void ClearCache()
    {
      Cache.Clear();
    }
  }
}

public static class DynamicCachedEvaluatorExtension
{
  public static DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
    WithCache<TGenotype, TSearchSpace, TProblem, TKey>(this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
                                                       TProblem problem,
                                                       Func<TGenotype, TKey>? keySelector = null)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class
    where TKey : notnull

  {
    return new DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>(evaluator, problem, keySelector);
  }

  public static DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>
    WithCache<TGenotype, TSearchSpace, TProblem>(this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
                                                 TProblem problem)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class

  {
    return new DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>(evaluator, problem, x => x);
  }

  public static DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
    WithCache<TGenotype, TSearchSpace, TProblem, TKey>(this TProblem problem, Func<TGenotype, TKey> keySelector)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class
    where TKey : notnull => new(new DirectEvaluator<TGenotype>(), problem, keySelector);

  public static DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>
    WithCache<TGenotype, TSearchSpace, TProblem>(this TProblem problem)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : DynamicProblem<TGenotype, TSearchSpace>
    where TGenotype : class
    => new(new DirectEvaluator<TGenotype>(), problem, x => x);
}
