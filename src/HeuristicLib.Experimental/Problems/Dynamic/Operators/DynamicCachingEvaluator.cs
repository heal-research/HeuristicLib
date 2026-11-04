using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Caches evaluation results for the source problem's current epoch.
/// </summary>
/// <remarks>
/// An epoch change invalidates cached results before the next lookup. If the epoch changes during a child evaluation,
/// its results are returned but not cached. The batch is not retried, and results already obtained from cache are not reevaluated.
/// </remarks>
public sealed record DynamicCachingEvaluator<TCandidate, TSearchSpace, TKey>
    : WrappingOperator<Evaluation, TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TCandidate : notnull
    where TKey : notnull
{
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

    public DynamicCachingEvaluator(IOperator<Evaluation, TCandidate> childEvaluator, IDynamicProblem<TCandidate, TSearchSpace> problem, ICacheKeySelector<TCandidate, TKey> keySelector)
        : base(childEvaluator)
    {
        SourceProblem = problem;
        KeySelector = keySelector;
    }

    /// <summary>
    /// Gets the number of cached candidate evaluations across consecutive all-hit batches that advances the problem epoch.
    /// A batch containing any cache miss resets the accumulated count. The default effectively disables cache-driven epoch advancement.
    /// The cache requests at most one update per epoch, even when the problem defers applying it.
    /// </summary>
    public long GraceCount { get; init; } = long.MaxValue;

    /// <remarks>
    /// This evaluator serves exactly one problem instance, matched by identity, which <c>Evaluate</c> checks.
    /// </remarks>
    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        if (SizeLimit < 0)
            throw new InvalidOperationException("The evaluation cache size limit must be nonnegative.");
        var cache = new DynamicEvaluationCache<TKey>(SizeLimit, GraceCount);
        var childEvaluator = scope.Resolve(ChildOperator);
        return new Executor<TRunSearchSpace, TRunProblem>(childEvaluator, SourceProblem, KeySelector, cache);
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem>(
        IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator,
        IDynamicProblem<TCandidate, TSearchSpace> sourceProblem,
        ICacheKeySelector<TCandidate, TKey> keySelector,
        DynamicEvaluationCache<TKey> cache) : IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic caching evaluator executions can only evaluate the dynamic problem they were created for.");
            return cache.Evaluate(candidates, keySelector, sourceProblem,
                missing => childEvaluator.Evaluate(missing, random, searchSpace, problem));
        }
    }
}

public static class DynamicCachedEvaluatorExtension
{
    extension<TCandidate>(IOperator<Evaluation, TCandidate> evaluator) where TCandidate : class
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
