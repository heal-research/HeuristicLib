using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TCandidate : notnull
    where TKey : notnull
{
    private sealed class ExecutionData
    {
        public MemoryCache Cache { get; }
        public long HitCount { get; set; }

        public ExecutionData(long? sizeLimit)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });
        }
    }

    /// <summary>
    /// Gets the dynamic problem whose epoch changes clear this evaluator's cache.
    /// </summary>
    public TProblem SourceProblem { get; init; }

    /// <summary>
    /// Gets the strategy that selects a candidate's cache key. Candidates that produce equal keys share one cached evaluation result.
    /// </summary>
    public ICacheKeySelector<TCandidate, TKey> KeySelector { get; init; }

    /// <summary>
    /// Gets the maximum number of cached evaluation results, or <see langword="null"/> when the cache has no configured size limit.
    /// </summary>
    public long? SizeLimit { get; init; }

    public DynamicCachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, TProblem problem, ICacheKeySelector<TCandidate, TKey> keySelector)
        : base(childEvaluator)
    {
        SourceProblem = problem;
        KeySelector = keySelector;
    }

    /// <summary>
    /// Gets the number of cached candidate evaluations across consecutive all-hit batches that advances the problem epoch.
    /// A batch containing any cache miss resets the accumulated count. The default effectively disables cache-driven epoch advancement.
    /// </summary>
    public long GraceCount { get; init; } = long.MaxValue;

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator) =>
        new Instance(childEvaluator, SourceProblem, KeySelector, SizeLimit, GraceCount);

    private sealed class Instance : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        private readonly ExecutionData executionData;
        private readonly ICacheKeySelector<TCandidate, TKey> keySelector;
        private readonly long graceCount;

        public Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, TProblem sourceProblem, ICacheKeySelector<TCandidate, TKey> keySelector, long? sizeLimit, long graceCount)
            : base(childEvaluator)
        {
            executionData = new ExecutionData(sizeLimit);
            this.keySelector = keySelector;
            this.graceCount = graceCount;
            sourceProblem.EpochClock.OnEpochChange += (_, _) =>
            {
                executionData.Cache.Clear();
                executionData.HitCount = 0;
            };
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var cache = executionData.Cache;
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
                var key = keySelector.SelectKey(candidate);

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
                var newObjectiveVectors = ChildEvaluator.Evaluate(uncachedCandidates, random, searchSpace, problem);
                for (var k = 0; k < uncachedKeys.Count; k++)
                {
                    cache.Set(uncachedKeys[k], newObjectiveVectors[k], new MemoryCacheEntryOptions { Size = 1 });
                }

                foreach (var (_, entry) in uncachedMap)
                {
                    var objectiveVector = newObjectiveVectors[entry.j];
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
                executionData.HitCount += cachedSolutionsCount;
                if (executionData.HitCount >= graceCount)
                {
                    problem.EpochClock.AdvanceEpoch();
                }
            }
            else
            {
                executionData.HitCount = 0;
            }

            return results;
        }
    }
}

public static class DynamicCachedEvaluatorExtension
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache<TKey>(TProblem problem, ICacheKeySelector<TCandidate, TKey> keySelector) where TKey : notnull
        {
            return new(evaluator, problem, keySelector);
        }

        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate> WithCache(TProblem problem)
        {
            return new(evaluator, problem, CacheKeySelection<TCandidate>.Identity);
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TKey>(TProblem problem) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : DynamicProblem<TCandidate, TSearchSpace> where TKey : notnull
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache(ICacheKeySelector<TCandidate, TKey> keySelector) => new(new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>(), problem, keySelector);
    }

    extension<TCandidate, TSearchSpace, TProblem>(TProblem problem) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate> WithCache() => new(new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>(), problem, CacheKeySelection<TCandidate>.Identity);
    }
}
