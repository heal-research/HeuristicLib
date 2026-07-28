using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public record DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
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

    private readonly TProblem sourceProblem;
    private readonly Func<TCandidate, TKey> keySelector;
    private readonly long? sizeLimit;

    public DynamicCachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, TProblem problem, Func<TCandidate, TKey> keySelector, long? sizeLimit = null)
        : base(evaluator)
    {
        sourceProblem = problem;
        this.keySelector = keySelector;
        this.sizeLimit = sizeLimit;
    }

    public long GraceCount { get; init; } = long.MaxValue;

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, sourceProblem, keySelector, sizeLimit, GraceCount);

    private sealed class Instance : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        private readonly ExecutionData executionData;
        private readonly Func<TCandidate, TKey> keySelector;
        private readonly long graceCount;

        public Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, TProblem sourceProblem, Func<TCandidate, TKey> keySelector, long? sizeLimit, long graceCount)
            : base(innerEvaluator)
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
                var newObjectives = InnerEvaluator.Evaluate(uncachedCandidates, random, searchSpace, problem);
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
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache<TKey>(TProblem problem, Func<TCandidate, TKey> keySelector) where TKey : notnull
        {
            return new(evaluator, problem, keySelector);
        }

        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate> WithCache(TProblem problem)
        {
            return new(evaluator, problem, x => x);
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TKey>(TProblem problem) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : DynamicProblem<TCandidate, TSearchSpace> where TKey : notnull
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache(Func<TCandidate, TKey> keySelector) => new(DirectEvaluator.For(problem), problem, keySelector);
    }

    extension<TCandidate, TSearchSpace, TProblem>(TProblem problem) where TCandidate : class where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate> WithCache() => new(DirectEvaluator.For(problem), problem, x => x);
    }
}
