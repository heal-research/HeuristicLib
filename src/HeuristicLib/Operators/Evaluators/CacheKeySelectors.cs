namespace HEAL.HeuristicLib.Operators.Evaluators;

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
