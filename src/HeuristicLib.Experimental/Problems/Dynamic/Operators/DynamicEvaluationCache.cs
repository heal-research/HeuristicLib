using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>A managed cache coordinating entries, epoch and consecutive all hit accounting.</summary>
/// <remarks>
/// All participants must use the same source problem and grace count, with compatible key and evaluation semantics.
/// Storage and accounting are synchronized together. Child evaluations run outside the lock and may overlap or duplicate
/// work. An evaluation spanning an epoch change is returned but never published into the new epoch.
/// Key selectors, child evaluators and the source problem must support the concurrency the author permits.
/// </remarks>
public sealed class DynamicEvaluationCache<TKey> where TKey : notnull
{
    private readonly Lock sync = new();
    private readonly EvaluationCache<TKey> entries;
    private object? source;
    private long graceCount;
    private int epoch = -1;
    private long hitCount;
    private bool updateRequested;

    public DynamicEvaluationCache(long? sizeLimit = null)
    {
        entries = new(sizeLimit);
    }

    internal void Bind(object sourceProblem, long grace)
    {
        lock (sync)
        {
            if (source is not null && (!ReferenceEquals(source, sourceProblem) || graceCount != grace))
                throw new InvalidOperationException("A dynamic cache can only be shared by the same source problem and grace count.");
            source = sourceProblem;
            graceCount = grace;
        }
    }

    private void SynchronizeEpoch(int currentEpoch)
    {
        if (epoch == currentEpoch)
            return;
        entries.Clear();
        hitCount = 0;
        updateRequested = false;
        epoch = currentEpoch;
    }

    internal IReadOnlyList<ObjectiveVector> Evaluate<TCandidate, TSearchSpace>(IReadOnlyList<TCandidate> candidates,
        ICacheKeySelector<TCandidate, TKey> keySelector, IDynamicProblem<TCandidate, TSearchSpace> problem,
        Func<IReadOnlyList<TCandidate>, IReadOnlyList<ObjectiveVector>> evaluate)
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        var keys = candidates.Select(keySelector.SelectKey).ToArray();
        var results = new ObjectiveVector[candidates.Count];
        var missingCandidates = new List<TCandidate>();
        var missingKeys = new List<TKey>();
        var positions = new Dictionary<TKey, (int Index, List<int> Positions)>();
        int evaluationEpoch;
        lock (sync)
        {
            evaluationEpoch = problem.CurrentEpoch;
            SynchronizeEpoch(evaluationEpoch);
            for (var i = 0; i < candidates.Count; i++)
            {
                if (entries.TryGet(keys[i], out var value))
                    results[i] = value;
                else if (positions.TryGetValue(keys[i], out var existing))
                    existing.Positions.Add(i);
                else
                {
                    positions.Add(keys[i], (missingCandidates.Count, [i]));
                    missingCandidates.Add(candidates[i]);
                    missingKeys.Add(keys[i]);
                }
            }
        }

        IReadOnlyList<ObjectiveVector>? evaluated = null;
        if (missingCandidates.Count > 0)
        {
            try
            {
                evaluated = evaluate(missingCandidates);
            }
            catch
            {
                lock (sync)
                    SynchronizeEpoch(problem.CurrentEpoch);
                throw;
            }
        }

        var requestUpdate = false;
        lock (sync)
        {
            SynchronizeEpoch(problem.CurrentEpoch);
            if (epoch == evaluationEpoch)
            {
                if (evaluated is not null)
                {
                    for (var i = 0; i < missingKeys.Count; i++)
                        entries.Set(missingKeys[i], evaluated[i]);
                    hitCount = 0;
                }
                else if (candidates.Count > 0)
                {
                    hitCount = candidates.Count > long.MaxValue - hitCount ? long.MaxValue : hitCount + candidates.Count;
                    requestUpdate = !updateRequested && hitCount >= graceCount;
                    if (requestUpdate)
                        updateRequested = true;
                }
            }
        }
        // The problem checks the epoch atomically with the request. Never call into its lock while holding ours.
        if (requestUpdate)
            ((IUpdateRequestable)problem).RequestUpdate(evaluationEpoch);
        if (evaluated is not null)
            foreach (var entry in positions.Values)
                foreach (var position in entry.Positions)
                    results[position] = evaluated[entry.Index];
        return results;
    }
}

public sealed class DynamicEvaluationCacheHandle<TKey> : ResourceHandle<DynamicEvaluationCache<TKey>> where TKey : notnull
{
    public long? SizeLimit { get; init; }

    protected override DynamicEvaluationCache<TKey> CreateResource()
    {
        if (SizeLimit < 0)
            throw new InvalidOperationException("The evaluation cache size limit must be nonnegative.");
        return new(SizeLimit);
    }
}
