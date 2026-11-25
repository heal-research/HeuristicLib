using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record CachingEvaluator<TCandidate, TKey>
    : WrappingOperator<Evaluation, TCandidate>
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

    public static Parameter<CachingEvaluator<TCandidate, TKey>, ICacheKeySelector<TCandidate, TKey>> KeySelectorParameter { get; } =
        new(nameof(KeySelector), static evaluator => evaluator.KeySelector, static (evaluator, value) => evaluator with { KeySelector = value });

    public static Parameter<CachingEvaluator<TCandidate, TKey>, long?> SizeLimitParameter { get; } =
        new(nameof(SizeLimit), static evaluator => evaluator.SizeLimit, static (evaluator, value) => evaluator with { SizeLimit = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [KeySelectorParameter, SizeLimitParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public CachingEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector)
        : base(childEvaluator)
    {
        KeySelector = keySelector;
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        if (SizeLimit < 0)
            throw new InvalidOperationException("The evaluation cache size limit must be nonnegative.");
        var cache = new EvaluationCache<TKey>(SizeLimit);
        var childEvaluator = scope.Resolve(ChildOperator);
        return new Executor(childEvaluator, KeySelector, cache);
    }

    private sealed class Executor(IEvaluationExecutor<TCandidate> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector, EvaluationCache<TKey> cache)
        : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
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

                if (cache.TryGet(key, out var cached))
                {
                    results[i] = cached;
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

            var newObjectiveVectors = childEvaluator.Evaluate(uncachedCandidates, random);

            for (var k = 0; k < uncachedKeys.Count; k++)
            {
                cache.Set(uncachedKeys[k], newObjectiveVectors[k]);
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

public sealed record CachingEvaluator<TCandidate>
    : CachingEvaluator<TCandidate, TCandidate>
    where TCandidate : notnull
{
    public CachingEvaluator(IOperator<Evaluation, TCandidate> childEvaluator)
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
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator)
        where TCandidate : notnull
    {
        public CachingEvaluator<TCandidate, TKey> Cached<TKey>(ICacheKeySelector<TCandidate, TKey> keySelector, long? sizeLimit = null) where TKey : notnull =>
            new(evaluator, keySelector) { SizeLimit = sizeLimit };

        public CachingEvaluator<TCandidate> Cached(long? sizeLimit = null) =>
            new(evaluator) { SizeLimit = sizeLimit };
    }
}
