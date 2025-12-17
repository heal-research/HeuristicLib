using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class CachedEvaluator<TGenotype, TEncoding, TProblem, TKey>(
  Func<TGenotype, TKey> keySelector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator)
  : BatchEvaluator<TGenotype, TEncoding, TProblem>
  where TKey : notnull
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public Func<TGenotype, TKey> KeySelector { get; } = keySelector;

  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; } = evaluator;

  protected readonly Dictionary<TKey, ObjectiveVector> Cache = [];

  // Core logic used by both this class and DynamicCachedEvaluator
  protected (ObjectiveVector[] Results, int UniqueEvaluatedCount, int CachedSolutionsCount)
    EvaluateWithCache(
      IReadOnlyList<TGenotype> solutions,
      IRandomNumberGenerator random,
      TEncoding encoding,
      TProblem problem) {
    var n = solutions.Count;
    if (n == 0)
      return (Array.Empty<ObjectiveVector>(), 0, 0);

    var results = new ObjectiveVector[n];

    var evalIndexBySolutionIndex = new int[n];
    for (var i = 0; i < n; i++)
      evalIndexBySolutionIndex[i] = -1;

    // For deduplication of uncached keys within this batch:
    // key -> index in `toEvaluate`
    var keyToEvalIndex = new Dictionary<TKey, int>();
    var toEvaluate = new List<TGenotype>();

    var cachedSolutionsCount = 0;

    // First pass: fill from cache where possible, and
    // build the de-duplicated list of solutions to evaluate.
    for (var i = 0; i < n; i++) {
      var solution = solutions[i];
      var key = KeySelector(solution);

      if (Cache.TryGetValue(key, out var cachedObjVec)) {
        // Seen before: use cached result
        results[i] = cachedObjVec;
        cachedSolutionsCount++;
      } else {
        // Not in cache yet: check if we've already
        // scheduled this key for evaluation in this batch
        if (!keyToEvalIndex.TryGetValue(key, out var evalIndex)) {
          evalIndex = toEvaluate.Count;
          toEvaluate.Add(solution);
          keyToEvalIndex[key] = evalIndex;
        }

        // Mark that this solution index uses `toEvaluate[evalIndex]`
        evalIndexBySolutionIndex[i] = evalIndex;
      }
    }

    var uniqueEvaluatedCount = toEvaluate.Count;

    // If everything was cached, we're done
    if (uniqueEvaluatedCount == 0)
      return (results, 0, cachedSolutionsCount);

    // Single batched call into the inner evaluator for all
    // unique uncached solutions
    var evaluated = Evaluator.Evaluate(toEvaluate, random, encoding, problem);

    // Update cache for all newly evaluated keys
    foreach (var (key, evalIndex) in keyToEvalIndex) {
      Cache[key] = evaluated[evalIndex];
    }

    // Second pass: fill in results for those that were not cached
    for (var i = 0; i < n; i++) {
      var evalIndex = evalIndexBySolutionIndex[i];
      if (evalIndex >= 0) {
        results[i] = evaluated[evalIndex];
      }
    }

    return (results, uniqueEvaluatedCount, cachedSolutionsCount);
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TEncoding encoding,
    TProblem problem) {
    var (results, _, _) = EvaluateWithCache(solutions, random, encoding, problem);
    return results;
  }

  public void ClearCache() => Cache.Clear();
}
