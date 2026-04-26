using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using Microsoft.Extensions.Caching.Memory;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record CachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>
  : WrappingEvaluator<TGenotype, TSearchSpace, TProblem, CachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : notnull
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

  protected readonly Func<TGenotype, TKey> KeySelector;
  protected readonly long? SizeLimit;

  public CachingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, Func<TGenotype, TKey> keySelector, long? sizeLimit = null)
    : base(evaluator)
  {
    KeySelector = keySelector;
    SizeLimit = sizeLimit;
  }

  protected override ExecutionState CreateInitialState() => new(SizeLimit);

  protected override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> genotypes,
    ExecutionState executionState,
    InnerEvaluate innerEvaluate,
    IRandomNumberGenerator random,
    TSearchSpace searchSpace,
    TProblem problem)
  {
    var cache = executionState.Cache;
    var n = genotypes.Count;
    var results = new ObjectiveVector[n];

    var uncachedGenotypes = new List<TGenotype>();
    var uncachedKeys = new List<TKey>();
    var uncachedMap = new Dictionary<TKey, (int j, List<int> indices)>();

    for (var i = 0; i < n; i++) {
      var genotype = genotypes[i];
      var key = KeySelector(genotype);

      if (cache.TryGetValue(key, out ObjectiveVector? cached)) {
        results[i] = cached!;
        continue;
      }

      if (!uncachedMap.TryGetValue(key, out var entry)) {
        var j = uncachedGenotypes.Count;
        uncachedGenotypes.Add(genotype);
        uncachedKeys.Add(key);
        uncachedMap.Add(key, (j, [i]));
      } else {
        entry.indices.Add(i);
      }
    }

    if (uncachedGenotypes.Count == 0) {
      return results;
    }

    var newObjectives = innerEvaluate(uncachedGenotypes, random, searchSpace, problem);

    for (var k = 0; k < uncachedKeys.Count; k++) {
      cache.Set(uncachedKeys[k], newObjectives[k], new MemoryCacheEntryOptions { Size = 1 });
    }

    foreach (var (_, entry) in uncachedMap) {
      var objectiveVector = newObjectives[entry.j];
      foreach (var i in entry.indices) {
        results[i] = objectiveVector;
      }
    }

    return results;
  }
}

public record CachingEvaluator<TGenotype, TSearchSpace, TProblem>
  : CachingEvaluator<TGenotype, TSearchSpace, TProblem, TGenotype>
  where TGenotype : notnull
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public CachingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, long? sizeLimit = null) : base(evaluator, x => x, sizeLimit) { }
}

public static class CachedEvaluatorExtensions
{
  extension<TGenotype, TSearchSpace, TProblem>(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator)
    where TGenotype : notnull
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public CachingEvaluator<TGenotype, TSearchSpace, TProblem, TKey> WithCache<TKey>(Func<TGenotype, TKey> keySelector, long? sizeLimit = null) where TKey : notnull
      => new(evaluator, keySelector, sizeLimit);

    public CachingEvaluator<TGenotype, TSearchSpace, TProblem> WithCache(long? sizeLimit = null)
      => new(evaluator, sizeLimit);
  }
}
