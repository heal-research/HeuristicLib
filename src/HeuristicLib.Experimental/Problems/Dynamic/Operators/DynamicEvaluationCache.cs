using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>A managed cache coordinating entries, epoch and consecutive all hit accounting.</summary>
/// <remarks>
/// Each dynamic caching evaluator executor owns one cache. Its calls may be concurrent, for example when several uses
/// share the evaluator's configuration. Storage and accounting are synchronized together. Child evaluations run
/// outside the lock and may overlap or duplicate work. An evaluation spanning an epoch change is returned but never
/// published into the new epoch. Key selectors, child evaluators and the source problem must support the concurrency
/// the author permits.
/// </remarks>
internal sealed class DynamicEvaluationCache<TKey> where TKey : notnull
{
    private readonly Lock sync = new();
    private readonly EvaluationCache<TKey> entries;
    private readonly long graceCount;
    private int epoch = -1;
    private long hitCount;
    private bool updateRequested;

    public DynamicEvaluationCache(long? sizeLimit, long graceCount)
    {
        entries = new(sizeLimit);
        this.graceCount = graceCount;
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
