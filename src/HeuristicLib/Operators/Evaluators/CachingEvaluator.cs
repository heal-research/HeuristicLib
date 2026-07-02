using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record CachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem, CachingEvaluator<TCandidate, TSearchSpace, TProblem, TKey>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TCandidate : notnull
  where TKey : notnull
{
    public sealed class ExecutionState
    {
        public MemoryCache Cache { get; }

        public ExecutionState(long? sizeLimit)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = true });
        }
    }

    protected readonly Func<TCandidate, TKey> KeySelector;
    protected readonly long? SizeLimit;

    public CachingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, Func<TCandidate, TKey> keySelector, long? sizeLimit = null)
      : base(evaluator)
    {
        KeySelector = keySelector;
        SizeLimit = sizeLimit;
    }

    protected override ExecutionState CreateInitialState() => new(SizeLimit);

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<TCandidate> candidates,
      ExecutionState executionState,
      InnerEvaluate innerEvaluate,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
    {
        var cache = executionState.Cache;
        var n = candidates.Count;
        var results = new ObjectiveVector[n];

        var uncachedCandidates = new List<TCandidate>();
        var uncachedKeys = new List<TKey>();
        var uncachedMap = new Dictionary<TKey, (int j, List<int> indices)>();

        for (var i = 0; i < n; i++)
        {
            var candidate = candidates[i];
            var key = KeySelector(candidate);

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

        return results;
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
