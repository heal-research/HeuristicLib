using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : DecoratorEvaluator<TGenotype, TSearchSpace, TProblem, DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TSearchSpace>
  where TGenotype : notnull
  where TKey : notnull
{
  public sealed class State
  {
    public MemoryCache Cache { get; }
    public long HitCount { get; set; }

    public State(long? sizeLimit)
    {
      Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });
    }
  }

  private readonly TProblem sourceProblem;
  private readonly Func<TGenotype, TKey> keySelector;
  private readonly long? sizeLimit;

  public DynamicCachingEvaluator(
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
    TProblem problem,
    Func<TGenotype, TKey> keySelector, long? sizeLimit = null)
    : base(evaluator)
  {
    sourceProblem = problem;
    this.keySelector = keySelector;
    this.sizeLimit = sizeLimit;
  }

  public long GraceCount { get; init; } = long.MaxValue;

  protected override State CreateInitialState()
  {
    var state = new State(sizeLimit);
    sourceProblem.EpochClock.OnEpochChange += (_, _) => {
      state.Cache.Clear();
      state.HitCount = 0;
    };
    return state;
  }

  protected override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> genotypes,
    State state,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> innerEvaluator,
    IRandomNumberGenerator random,
    TSearchSpace searchSpace,
    TProblem problem)
  {
    var cache = state.Cache;
    var beforeCacheStatistics = cache.GetCurrentStatistics();
    var beforeHits = beforeCacheStatistics?.TotalHits ?? 0;
    var beforeMisses = beforeCacheStatistics?.TotalMisses ?? 0;

    var n = genotypes.Count;
    var results = new ObjectiveVector[n];

    var uncachedGenotypes = new List<TGenotype>();
    var uncachedKeys = new List<TKey>();
    var uncachedMap = new Dictionary<TKey, (int j, List<int> indices)>();

    for (var i = 0; i < n; i++) {
      var genotype = genotypes[i];
      var key = keySelector(genotype);

      if (cache.TryGetValue(key, out ObjectiveVector? cached)) {
        results[i] = cached!;
        continue;
      }

      if (!uncachedMap.TryGetValue(key, out var entry)) {
        var j = uncachedGenotypes.Count;
        uncachedGenotypes.Add(genotype);
        uncachedKeys.Add(key);
        uncachedMap.Add(key, (j, [i]));
      } else {
        entry.indices.Add(i);
      }
    }

    if (uncachedGenotypes.Count > 0) {
      var newObjectives = innerEvaluator.Evaluate(uncachedGenotypes, random, searchSpace, problem);
      for (var k = 0; k < uncachedKeys.Count; k++) {
        cache.Set(uncachedKeys[k], newObjectives[k], new MemoryCacheEntryOptions { Size = 1 });
      }

      foreach (var (_, entry) in uncachedMap) {
        var objectiveVector = newObjectives[entry.j];
        foreach (var i in entry.indices) {
          results[i] = objectiveVector;
        }
      }
    }

    var afterCacheStatistics = cache.GetCurrentStatistics();
    var afterHits = afterCacheStatistics?.TotalHits ?? 0;
    var afterMisses = afterCacheStatistics?.TotalMisses ?? 0;

    var uniqueEvaluatedCount = afterMisses - beforeMisses;
    var cachedSolutionsCount = afterHits - beforeHits;

    if (genotypes.Count == 0) {
      return results;
    }

    if (uniqueEvaluatedCount == 0) {
      state.HitCount += cachedSolutionsCount;
      if (state.HitCount >= GraceCount) {
        problem.EpochClock.AdvanceEpoch();
      }
    } else {
      state.HitCount = 0;
    }

    return results;
  }
}

public static class DynamicCachedEvaluatorExtension
{
  public static DynamicCachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
    WithCache<TGenotype, TSearchSpace, TProblem, TKey>(this IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
                                                       TProblem problem,
                                                       Func<TGenotype, TKey> keySelector)
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
