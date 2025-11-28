using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class CachedEvaluator<TGenotype, TEncoding, TProblem, TKey> : BatchEvaluator<TGenotype, TEncoding, TProblem> where TKey : notnull where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> {
  public CachedEvaluator(Func<TGenotype, TKey> keySelector, IEvaluator<TGenotype, TEncoding, TProblem> evaluator) {
    KeySelector = keySelector;
    Evaluator = evaluator;
  }

  public Func<TGenotype, TKey> KeySelector { get; }

  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; }

  protected readonly Dictionary<TKey, ObjectiveVector> Cache = [];

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TEncoding encoding,
    TProblem problem) {
    int n = solutions.Count;
    if (n == 0) return [];

    var results = new ObjectiveVector[n];

    var evalIndexBySolutionIndex = new int[n];
    for (int i = 0; i < n; i++)
      evalIndexBySolutionIndex[i] = -1;

    // For deduplication of uncached keys within this batch:
    // key -> index in `toEvaluate`
    var keyToEvalIndex = new Dictionary<TKey, int>();

    var toEvaluate = new List<TGenotype>();

    // First pass: fill from cache where possible, and
    // build the de-duplicated list of solutions to evaluate.
    for (int i = 0; i < n; i++) {
      var solution = solutions[i];
      var key = KeySelector(solution);

      if (Cache.TryGetValue(key, out var cachedObjVec)) {
        // Seen before: use cached result
        results[i] = cachedObjVec;
      } else {
        // Not in cache yet: check if we've already
        // scheduled this key for evaluation in this batch
        if (!keyToEvalIndex.TryGetValue(key, out int evalIndex)) {
          evalIndex = toEvaluate.Count;
          toEvaluate.Add(solution);
          keyToEvalIndex[key] = evalIndex;
        }

        // Mark that this solution index uses `toEvaluate[evalIndex]`
        evalIndexBySolutionIndex[i] = evalIndex;
      }
    }

    // If everything was cached, we're done
    if (toEvaluate.Count == 0)
      return results;

    // Single batched call into the inner evaluator for all
    // unique uncached solutions
    var evaluated = Evaluator.Evaluate(toEvaluate, random, encoding, problem);

    // Update cache for all newly evaluated keys
    foreach (var (key, evalIndex) in keyToEvalIndex) {
      Cache[key] = evaluated[evalIndex];
    }

    // Second pass: fill in results for those that were not cached
    for (int i = 0; i < n; i++) {
      int evalIndex = evalIndexBySolutionIndex[i];
      if (evalIndex >= 0) {
        results[i] = evaluated[evalIndex];
      }
    }

    return results;
  }

  public void ClearCache() => Cache.Clear();
}

public class DynamicCachedEvaluator<TGenotype, TEncoding, TProblem, TKey> : CachedEvaluator<TGenotype, TEncoding, TProblem, TKey>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : DynamicProblem<TGenotype, TEncoding>
  where TKey : notnull
  where TGenotype : class {
  public int GraceCount { get; init; } = int.MaxValue;
  private int HitCount = 0;

  public DynamicCachedEvaluator(Func<TGenotype, TKey> keySelector, IEvaluator<TGenotype, TEncoding, TProblem> evaluator, TProblem problem) : base(keySelector, evaluator) {
    problem.EpochClock.OnEpochChange += (_, _) => ClearCache();
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<TGenotype> solutions,
    IRandomNumberGenerator random,
    TEncoding encoding,
    TProblem problem) {
    int n = solutions.Count;
    if (n == 0)
      return [];

    var results = new ObjectiveVector[n];

    var evalIndexBySolutionIndex = new int[n];
    for (int i = 0; i < n; i++)
      evalIndexBySolutionIndex[i] = -1;

    // For deduplication of uncached keys within this batch:
    // key -> index in `toEvaluate`
    var keyToEvalIndex = new Dictionary<TKey, int>();

    var toEvaluate = new List<TGenotype>();

    // First pass: fill from cache where possible, and
    // build the de-duplicated list of solutions to evaluate.
    for (int i = 0; i < n; i++) {
      var solution = solutions[i];
      var key = KeySelector(solution);

      if (Cache.TryGetValue(key, out var cachedObjVec)) {
        // Seen before: use cached result
        results[i] = cachedObjVec;
      } else {
        // Not in cache yet: check if we've already
        // scheduled this key for evaluation in this batch
        if (!keyToEvalIndex.TryGetValue(key, out int evalIndex)) {
          evalIndex = toEvaluate.Count;
          toEvaluate.Add(solution);
          keyToEvalIndex[key] = evalIndex;
        }

        // Mark that this solution index uses `toEvaluate[evalIndex]`
        evalIndexBySolutionIndex[i] = evalIndex;
      }
    }

    // If everything was cached, we're done
    if (toEvaluate.Count == 0) {
      HitCount += results.Length;
      if (HitCount >= GraceCount) {
        problem.EpochClock.AdvanceEpoch();
      }

      return results;
    }

    HitCount = 0;

    // Single batched call into the inner evaluator for all
    // unique uncached solutions
    var evaluated = Evaluator.Evaluate(toEvaluate, random, encoding, problem);

    // Update cache for all newly evaluated keys
    foreach (var (key, evalIndex) in keyToEvalIndex) {
      Cache[key] = evaluated[evalIndex];
    }

    // Second pass: fill in results for those that were not cached
    for (int i = 0; i < n; i++) {
      int evalIndex = evalIndexBySolutionIndex[i];
      if (evalIndex >= 0) {
        results[i] = evaluated[evalIndex];
      }
    }

    return results;
  }
}
