using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record CachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TCandidate : notnull
    where TKey : notnull
{
    /// <summary>
    /// Gets the strategy that selects a candidate's cache key. Candidates that produce equal keys share one cached
    /// objective vector. Only use a key selector whose equal keys identify candidates that are interchangeable for
    /// evaluation.
    /// </summary>
    public ICacheKeySelector<TCandidate, TKey> KeySelector { get; init; }

    /// <summary>
    /// Gets the maximum number of cached evaluation results, or <see langword="null"/> when the cache has no configured size limit.
    /// </summary>
    public long? SizeLimit { get; init; }

    public CachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector)
        : base(childEvaluator)
    {
        KeySelector = keySelector;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator) =>
        new Instance(childEvaluator, KeySelector, SizeLimit);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector, long? sizeLimit)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
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

            if (uncachedCandidates.Count == 0)
            {
                return results;
            }

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

            return results;
        }
    }
}

public sealed record CachingEvaluator<TCandidate, TSearchSpace, TProblem>
    : CachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate>
    where TCandidate : notnull
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public CachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator)
        : base(childEvaluator, CacheKeySelection<TCandidate>.Identity)
    {
    }
}

/// <summary>
/// Selects the stable key by which candidate evaluations are cached.
/// </summary>
/// <typeparam name="TCandidate">The candidate type.</typeparam>
/// <typeparam name="TKey">The cache-key type.</typeparam>
public interface ICacheKeySelector<in TCandidate, out TKey>
    where TKey : notnull
{
    /// <summary>
    /// Selects the cache key for <paramref name="candidate"/>.
    /// Candidates that produce equal keys share one cached objective vector.
    /// </summary>
    TKey SelectKey(TCandidate candidate);
}

public sealed record IdentityCacheKeySelector<TCandidate> : ICacheKeySelector<TCandidate, TCandidate>
    where TCandidate : notnull
{
    public TCandidate SelectKey(TCandidate candidate) => candidate;
}

public static class CacheKeySelection<TCandidate>
    where TCandidate : notnull
{
    public static IdentityCacheKeySelector<TCandidate> Identity { get; } = new();
}

public static class CachingEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TCandidate : notnull
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache<TKey>(ICacheKeySelector<TCandidate, TKey> keySelector, long? sizeLimit = null) where TKey : notnull =>
            new(evaluator, keySelector) { SizeLimit = sizeLimit };

        public CachingEvaluator<TCandidate, TSearchSpace, TProblem> WithCache(long? sizeLimit = null) =>
            new(evaluator) { SizeLimit = sizeLimit };
    }
}
