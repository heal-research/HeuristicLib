using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem, DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : DynamicProblem<TCandidate, TSearchSpace>
  where TCandidate : notnull
  where TKey : notnull
{
    public sealed class ExecutionState
    {
        public MemoryCache Cache { get; }
        public long HitCount { get; set; }

        public ExecutionState(long? sizeLimit)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });
        }
    }

    private readonly TProblem sourceProblem;
    private readonly Func<TCandidate, TKey> keySelector;
    private readonly long? sizeLimit;

    public DynamicCachingEvaluator(
      IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
      TProblem problem,
      Func<TCandidate, TKey> keySelector, long? sizeLimit = null)
      : base(evaluator)
    {
        sourceProblem = problem;
        this.keySelector = keySelector;
        this.sizeLimit = sizeLimit;
    }

    public long GraceCount { get; init; } = long.MaxValue;

    protected override ExecutionState CreateInitialState()
    {
        var executionState = new ExecutionState(sizeLimit);
        sourceProblem.EpochClock.OnEpochChange += (_, _) =>
        {
            executionState.Cache.Clear();
            executionState.HitCount = 0;
        };
        return executionState;
    }

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<TCandidate> candidates,
      ExecutionState executionState,
      InnerEvaluate innerEvaluate,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
    {
        var cache = executionState.Cache;
        var beforeCacheStatistics = cache.GetCurrentStatistics();
        var beforeHits = beforeCacheStatistics?.TotalHits ?? 0;
        var beforeMisses = beforeCacheStatistics?.TotalMisses ?? 0;

        var n = candidates.Count;
        var results = new ObjectiveVector[n];

        var uncachedCandidates = new List<TCandidate>();
        var uncachedKeys = new List<TKey>();
        var uncachedMap = new Dictionary<TKey, (int j, List<int> indices)>();

        for (var i = 0; i < n; i++)
        {
            var candidate = candidates[i];
            var key = keySelector(candidate);

            if (cache.TryGetValue(key, out ObjectiveVector? cached))
            {
                results[i] = cached!;
                continue;
            }

            if (!uncachedMap.TryGetValue(key, out var entry))
            {
                var j = uncachedCandidates.Count;
                uncachedCandidates.Add(candidate);
                uncachedKeys.Add(key);
                uncachedMap.Add(key, (j, [i]));
            }
            else
            {
                entry.indices.Add(i);
            }
        }

        if (uncachedCandidates.Count > 0)
        {
            var newObjectives = innerEvaluate(uncachedCandidates, random, searchSpace, problem);
            for (var k = 0; k < uncachedKeys.Count; k++)
            {
                cache.Set(uncachedKeys[k], newObjectives[k], new MemoryCacheEntryOptions { Size = 1 });
            }

            foreach (var (_, entry) in uncachedMap)
            {
                var objectiveVector = newObjectives[entry.j];
                foreach (var i in entry.indices)
                {
                    results[i] = objectiveVector;
                }
            }
        }

        var afterCacheStatistics = cache.GetCurrentStatistics();
        var afterHits = afterCacheStatistics?.TotalHits ?? 0;
        var afterMisses = afterCacheStatistics?.TotalMisses ?? 0;

        var uniqueEvaluatedCount = afterMisses - beforeMisses;
        var cachedSolutionsCount = afterHits - beforeHits;

        if (candidates.Count == 0)
        {
            return results;
        }

        if (uniqueEvaluatedCount == 0)
        {
            executionState.HitCount += cachedSolutionsCount;
            if (executionState.HitCount >= GraceCount)
            {
                problem.EpochClock.AdvanceEpoch();
            }
        }
        else
        {
            executionState.HitCount = 0;
        }

        return results;
    }
}

public static class DynamicCachedEvaluatorExtension
{
    public static DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
      WithCache<TCandidate, TSearchSpace, TProblem, TKey>(this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
                                                         TProblem problem,
                                                         Func<TCandidate, TKey> keySelector)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : DynamicProblem<TCandidate, TSearchSpace>
      where TCandidate : class
      where TKey : notnull

    {
        return new DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>(evaluator, problem, keySelector);
    }

    public static DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate>
      WithCache<TCandidate, TSearchSpace, TProblem>(this IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
                                                   TProblem problem)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : DynamicProblem<TCandidate, TSearchSpace>
      where TCandidate : class

    {
        return new DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate>(evaluator, problem, x => x);
    }

    public static DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
      WithCache<TCandidate, TSearchSpace, TProblem, TKey>(this TProblem problem, Func<TCandidate, TKey> keySelector)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : DynamicProblem<TCandidate, TSearchSpace>
      where TCandidate : class
      where TKey : notnull => new(new DirectEvaluator<TCandidate>(), problem, keySelector);

    public static DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate>
      WithCache<TCandidate, TSearchSpace, TProblem>(this TProblem problem)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : DynamicProblem<TCandidate, TSearchSpace>
      where TCandidate : class
      => new(new DirectEvaluator<TCandidate>(), problem, x => x);
}
