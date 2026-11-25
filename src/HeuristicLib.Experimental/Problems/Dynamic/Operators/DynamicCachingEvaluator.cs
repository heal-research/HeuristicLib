using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Caches evaluation results for the bound problem's current epoch.
/// </summary>
/// <remarks>
/// An epoch change invalidates cached results before the next lookup. If the epoch changes during a child evaluation,
/// its results are returned but not cached. The batch is not retried, and results already obtained from cache are not reevaluated.
/// </remarks>
public sealed record DynamicCachingEvaluator<TCandidate, TKey>
    : WrappingOperator<Evaluation, TCandidate>
    where TCandidate : notnull
    where TKey : notnull
{
    /// <summary>
    /// Gets the strategy that selects a candidate's cache key. Candidates that produce equal keys share one cached evaluation result.
    /// </summary>
    public ICacheKeySelector<TCandidate, TKey> KeySelector { get; init; }

    /// <summary>
    /// Gets the maximum number of cached evaluation results, or <see langword="null"/> when the cache has no configured size limit.
    /// </summary>
    public long? SizeLimit { get; init; }

    /// <summary>
    /// Gets the number of cached candidate evaluations across consecutive all-hit batches that advances the problem epoch.
    /// A batch containing any cache miss resets the accumulated count. The default effectively disables cache-driven epoch advancement.
    /// The cache requests at most one update per epoch, even when the problem defers applying it.
    /// </summary>
    public long GraceCount { get; init; } = long.MaxValue;

    public static Parameter<DynamicCachingEvaluator<TCandidate, TKey>, ICacheKeySelector<TCandidate, TKey>> KeySelectorParameter { get; } =
        new(nameof(KeySelector), static evaluator => evaluator.KeySelector, static (evaluator, value) => evaluator with { KeySelector = value });

    public static Parameter<DynamicCachingEvaluator<TCandidate, TKey>, long?> SizeLimitParameter { get; } =
        new(nameof(SizeLimit), static evaluator => evaluator.SizeLimit, static (evaluator, value) => evaluator with { SizeLimit = value });

    public static Parameter<DynamicCachingEvaluator<TCandidate, TKey>, long> GraceCountParameter { get; } =
        new(nameof(GraceCount), static evaluator => evaluator.GraceCount, static (evaluator, value) => evaluator with { GraceCount = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [KeySelectorParameter, SizeLimitParameter, GraceCountParameter];

    public DynamicCachingEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector)
        : base(childEvaluator)
    {
        KeySelector = keySelector;
    }

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override bool Supports(ExecutionSignature execution) =>
        base.Supports(execution)
        && execution.Problem.IsAssignableTo(typeof(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>));

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        if (SizeLimit < 0)
            throw new InvalidOperationException("The evaluation cache size limit must be nonnegative.");
        // Supports has already accepted the run's problem, so it is a dynamic problem over TCandidate.
        if (scope.Problem is not IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem)
            throw ExecutionSignature.Mismatch(this,
                ExecutionSignature.Describe(typeof(ISearchSpace<TCandidate>), typeof(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>)),
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)));
        var cache = new DynamicEvaluationCache<TKey>(SizeLimit, GraceCount);
        return new Executor(scope.Resolve(ChildOperator), problem, KeySelector, cache);
    }

    private sealed class Executor(
        IEvaluationExecutor<TCandidate> childEvaluator,
        IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem,
        ICacheKeySelector<TCandidate, TKey> keySelector,
        DynamicEvaluationCache<TKey> cache) : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            return cache.Evaluate(candidates, keySelector, problem,
                missing => childEvaluator.Evaluate(missing, random));
        }
    }
}

public static class DynamicCachedEvaluatorExtension
{
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator) where TCandidate : class
    {
        public DynamicCachingEvaluator<TCandidate, TKey> CachedPerEpoch<TKey>(ICacheKeySelector<TCandidate, TKey> keySelector)
            where TKey : notnull
        {
            return new(evaluator, keySelector);
        }

        public DynamicCachingEvaluator<TCandidate, TCandidate> CachedPerEpoch()
        {
            return new(evaluator, CacheKeySelection<TCandidate>.Identity);
        }
    }
}
