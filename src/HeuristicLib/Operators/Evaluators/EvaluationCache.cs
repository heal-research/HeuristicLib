using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Operators;

/// <summary>A typed evaluation store using MemoryCache with an optional entry count limit.</summary>
/// <remarks>
/// Participants must agree on key interpretation and evaluation semantics. MemoryCache supplies key equality and eviction.
/// Storage is safe for concurrent callers. Concurrent misses may evaluate twice and admission is not guaranteed at capacity.
/// Statistics, linked entry tracking, expiration tokens and callbacks are not enabled. This restricted store needs no
/// deterministic cleanup and never holds a child executor or a requesting context.
/// </remarks>
public sealed class EvaluationCache<TKey> where TKey : notnull
{
    private readonly MemoryCache entries;

    public EvaluationCache(long? sizeLimit = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeLimit.GetValueOrDefault(), nameof(sizeLimit));
        // Thread local statistics can retain the cache on a live thread after its executor is discarded.
        entries = new(new MemoryCacheOptions { SizeLimit = sizeLimit, TrackStatistics = false, TrackLinkedCacheEntries = false });
    }

    public bool TryGet(TKey key, [NotNullWhen(true)] out ObjectiveVector? value) => entries.TryGetValue(key, out value);

    public void Set(TKey key, ObjectiveVector value) => entries.Set(key, value, new MemoryCacheEntryOptions { Size = 1 });

    public void Clear() => entries.Clear();
}

public sealed class EvaluationCacheHandle<TKey> : ResourceHandle<EvaluationCache<TKey>> where TKey : notnull
{
    public long? SizeLimit { get; init; }

    protected internal override EvaluationCache<TKey> CreateResource()
    {
        if (SizeLimit < 0)
            throw new InvalidOperationException("The evaluation cache size limit must be nonnegative.");
        return new(SizeLimit);
    }
}
