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
    private readonly Func<TCandidate, TKey> keySelector;
    private readonly long? sizeLimit;

    public CachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, Func<TCandidate, TKey> keySelector, long? sizeLimit = null)
      : base(evaluator)
    {
        this.keySelector = keySelector;
        this.sizeLimit = sizeLimit;
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateEvaluatorInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator) =>
        new Instance(innerEvaluator, keySelector, sizeLimit);

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator, Func<TCandidate, TKey> keySelector, long? sizeLimit)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(innerEvaluator)
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });

        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var n = candidates.Count;
            var results = new EvaluatedCandidate<TCandidate>[n];
            var uncachedCandidates = new List<TCandidate>();
            var uncachedKeys = new List<TKey>();
            var uncachedMap = new Dictionary<TKey, (int j, List<int> indices)>();

            for (var i = 0; i < n; i++)
            {
                var candidate = candidates[i];
                var key = keySelector(candidate);

                if (cache.TryGetValue(key, out EvaluatedCandidate<TCandidate>? cached))
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

            var newEvaluatedCandidates = InnerEvaluator.Evaluate(uncachedCandidates, random, searchSpace, problem);

            for (var k = 0; k < uncachedKeys.Count; k++)
            {
                cache.Set(uncachedKeys[k], newEvaluatedCandidates[k], new MemoryCacheEntryOptions { Size = 1 });
            }

            foreach (var (_, entry) in uncachedMap)
            {
                var evaluatedCandidate = newEvaluatedCandidates[entry.j];
                foreach (var i in entry.indices)
                {
                    results[i] = evaluatedCandidate;
                }
            }

            return results;
        }
    }
}

public record CachingEvaluator<TCandidate, TSearchSpace, TProblem>
    : CachingEvaluator<TCandidate, TSearchSpace, TProblem, TCandidate>
    where TCandidate : notnull
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public CachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, long? sizeLimit = null) : base(evaluator, x => x, sizeLimit) { }
}

public static class CachedEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TCandidate : notnull
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public CachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey> WithCache<TKey>(Func<TCandidate, TKey> keySelector, long? sizeLimit = null) where TKey : notnull
            => new(evaluator, keySelector, sizeLimit);

        public CachingEvaluator<TCandidate, TSearchSpace, TProblem> WithCache(long? sizeLimit = null)
            => new(evaluator, sizeLimit);
    }
}
