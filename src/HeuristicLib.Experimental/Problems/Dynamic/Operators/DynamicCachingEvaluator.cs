using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record DynamicCachingEvaluator<TCandidate, TSearchSpace, TKey>
    : WrappingEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
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
    public IDynamicProblem<TCandidate, TSearchSpace> SourceProblem { get; init; }

    /// <summary>
    /// Gets the strategy that selects a candidate's cache key. Candidates that produce equal keys share one cached evaluation result.
    /// </summary>
    public ICacheKeySelector<TCandidate, TKey> KeySelector { get; init; }

    /// <summary>
    /// Gets the maximum number of cached evaluation results, or <see langword="null"/> when the cache has no configured size limit.
    /// </summary>
    public long? SizeLimit { get; init; }

    public DynamicCachingEvaluator(IEvaluator<TCandidate> childEvaluator, IDynamicProblem<TCandidate, TSearchSpace> problem, ICacheKeySelector<TCandidate, TKey> keySelector)
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

    /// <remarks>
    /// This evaluator serves exactly one problem instance, matched by identity, which <c>Evaluate</c> checks.
    /// </remarks>
    protected override IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childEvaluator, SourceProblem, KeySelector, SizeLimit, GraceCount);

    private sealed class Instance<TRunSearchSpace, TRunProblem> : WrappingEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        private readonly ExecutionData executionData;
        private readonly IDynamicProblem<TCandidate, TSearchSpace> sourceProblem;
        private readonly ICacheKeySelector<TCandidate, TKey> keySelector;
        private readonly long graceCount;

        public Instance(IEvaluatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator, IDynamicProblem<TCandidate, TSearchSpace> sourceProblem, ICacheKeySelector<TCandidate, TKey> keySelector, long? sizeLimit, long graceCount)
            : base(childEvaluator)
        {
            executionData = new ExecutionData(sizeLimit);
            this.sourceProblem = sourceProblem;
            this.keySelector = keySelector;
            this.graceCount = graceCount;
            sourceProblem.EpochClock.OnEpochChange += (_, _) =>
            {
                executionData.Cache.Clear();
                executionData.HitCount = 0;
            };
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic caching evaluator instances can only evaluate the dynamic problem they were created for.");

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
                    sourceProblem.EpochClock.AdvanceEpoch();
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
    extension<TCandidate>(IEvaluator<TCandidate> evaluator) where TCandidate : class
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TKey> Cached<TSearchSpace, TKey>(IDynamicProblem<TCandidate, TSearchSpace> problem, ICacheKeySelector<TCandidate, TKey> keySelector)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TKey : notnull
        {
            return new(evaluator, problem, keySelector);
        }

        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TCandidate> Cached<TSearchSpace>(IDynamicProblem<TCandidate, TSearchSpace> problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            return new(evaluator, problem, CacheKeySelection<TCandidate>.Identity);
        }
    }

    extension<TCandidate, TSearchSpace>(IDynamicProblem<TCandidate, TSearchSpace> problem)
        where TCandidate : class
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TKey> Cached<TKey>(ICacheKeySelector<TCandidate, TKey> keySelector)
            where TKey : notnull =>
            new(new ProblemEvaluator<TCandidate>(), problem, keySelector);

        public DynamicCachingEvaluator<TCandidate, TSearchSpace, TCandidate> Cached() =>
            new(new ProblemEvaluator<TCandidate>(), problem, CacheKeySelection<TCandidate>.Identity);
    }
}
