using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Caches evaluation results for the source problem's current epoch.
/// </summary>
/// <remarks>
/// An epoch change invalidates cached results before the next lookup. If the epoch changes during a child evaluation,
/// its results are returned but not cached. The batch is not retried, and results already obtained from cache are not reevaluated.
/// </remarks>
public sealed record DynamicCachingEvaluator<TCandidate, TSearchSpace, TKey>
    : WrappingEvaluator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TCandidate : notnull
    where TKey : notnull
{
    private sealed class ExecutionState
    {
        public MemoryCache Cache { get; }
        public long HitCount { get; set; }
        public int Epoch { get; private set; }

        public ExecutionState(long? sizeLimit, int initialEpoch)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit });
            Epoch = initialEpoch;
        }

        public void SynchronizeEpoch(int epoch)
        {
            if (Epoch == epoch)
                return;
            Cache.Clear();
            HitCount = 0;
            Epoch = epoch;
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
    protected override WrapperExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
    {
        var state = new ExecutionState(SizeLimit, SourceProblem.CurrentEpoch);
        return childEvaluator => new Execution<TRunSearchSpace, TRunProblem>(childEvaluator, SourceProblem, KeySelector, GraceCount, state);
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem> : WrappingEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        private readonly ExecutionState state;
        private readonly IDynamicProblem<TCandidate, TSearchSpace> sourceProblem;
        private readonly ICacheKeySelector<TCandidate, TKey> keySelector;
        private readonly long graceCount;

        public Execution(IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator, IDynamicProblem<TCandidate, TSearchSpace> sourceProblem, ICacheKeySelector<TCandidate, TKey> keySelector, long graceCount, ExecutionState state)
            : base(childEvaluator)
        {
            this.state = state;
            this.sourceProblem = sourceProblem;
            this.keySelector = keySelector;
            this.graceCount = graceCount;
        }

        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TRunSearchSpace searchSpace, TRunProblem problem)
        {
            if (!ReferenceEquals(problem, sourceProblem))
                throw new InvalidOperationException("Dynamic caching evaluator executions can only evaluate the dynamic problem they were created for.");

            var epoch = sourceProblem.CurrentEpoch;
            state.SynchronizeEpoch(epoch);
            var cache = state.Cache;
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
                IReadOnlyList<ObjectiveVector> newObjectiveVectors;
                try
                {
                    newObjectiveVectors = ChildEvaluator.Evaluate(uncachedCandidates, random, searchSpace, problem);
                }
                finally
                {
                    state.SynchronizeEpoch(sourceProblem.CurrentEpoch);
                }

                // Objective vectors carry no epoch; a batch crossing an update cannot be safely admitted.
                if (state.Epoch == epoch)
                {
                    for (var k = 0; k < uncachedKeys.Count; k++)
                    {
                        cache.Set(uncachedKeys[k], newObjectiveVectors[k], new MemoryCacheEntryOptions { Size = 1 });
                    }
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

            if (n == 0)
            {
                return results;
            }

            if (uncachedCandidates.Count == 0)
            {
                state.HitCount += n;
                if (state.HitCount >= graceCount)
                {
                    ((IUpdateRequestable)sourceProblem).RequestUpdate();
                }
            }
            else
            {
                state.HitCount = 0;
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
