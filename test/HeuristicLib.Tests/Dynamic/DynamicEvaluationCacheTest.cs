using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Dynamic;

//genotypes must be class types
file sealed class DummyGenotype(int val) {
  public readonly int Value = val;
}

file sealed class DummyEncoding : IEncoding<DummyGenotype> {
  public bool Contains(DummyGenotype genotype) => true;
}

file sealed class DummyDynamicProblem : DynamicProblem<DummyGenotype, DummyEncoding> {
  public DummyDynamicProblem(IRandomNumberGenerator env, int epochLength)
    : base(env, updatePolicy: UpdatePolicy.AfterEvaluation, epochLength: epochLength) {
    SearchSpace = new DummyEncoding();
    Objective = SingleObjective.Minimize;
  }

  public override DummyEncoding SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(DummyGenotype solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    return solution.Value;
  }

  protected override void Update() { }
}

file sealed class CountingEvaluator : IEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem> {
  public int Calls { get; private set; }
  public int LastBatchSize { get; private set; }

  public IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<DummyGenotype> solutions,
    IRandomNumberGenerator random,
    DummyEncoding encoding,
    DummyDynamicProblem problem) {
    Calls++;
    LastBatchSize = solutions.Count;
    return solutions.Select(s => problem.Evaluate(s, random)).ToArray();
  }
}

public class DynamicEvaluationCacheTests {
  [Fact]
  public void DeduplicatesWithinBatch_EvaluatesOnce() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000); // avoid boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    );

    var res = cached.Evaluate(
      [new DummyGenotype(1), new DummyGenotype(1), new DummyGenotype(1)],
      NoRandomGenerator.Instance, problem.SearchSpace, problem);

    Assert.Equal(1, inner.Calls);
    Assert.Equal(1, inner.LastBatchSize);
    Assert.Equal([1.0, 1.0, 1.0], res.Select(v => v[0]).ToArray());

    // Only one real evaluation => one tick
    Assert.Equal(1L, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void CachesAcrossBatches_NoReevaluation_NoTick() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000); // avoid boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    );

    _ = cached.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    // Fully cached => inner evaluator not called => no ticking
    _ = cached.Evaluate([new DummyGenotype(2), new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);
  }

  [Fact]
  public void EpochBoundary_ClearsCache_ThenRequiresReevaluation() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 2); // boundary every 2 evals
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    );

    // Evaluate two distinct keys -> 2 evaluations -> should hit boundary and schedule an epoch change
    _ = cached.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);
    Assert.True(problem.EpochClock.PendingEpochs > 0);

    // resolve -> fires OnEpochChange -> cached evaluator clears cache
    problem.EpochClock.ResolvePendingEpochs(() => { });

    Assert.Equal(1, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);

    // Previously cached: now must be reevaluated
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(2, inner.Calls);
  }

  [Fact]
  public void GraceCount_Reached_AdvancesEpoch_WithoutTicking() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000); // avoid natural boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    ) { GraceCount = 3 };

    // Prime cache (causes 1 tick)
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(1L, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.CurrentEpoch);

    // Now do 3 cached batches => no ticking, but should force AdvanceEpoch
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks); // still 1 => proves no ticking on cache hit
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks); // still 1 => proves no ticking on cache hit
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);

    Assert.Equal(10_000L, problem.EpochClock.Ticks); // still 1 => proves no ticking on cache hit
    Assert.True(problem.EpochClock.PendingEpochs > 0); // epoch forced

    // resolve -> epoch changes -> cache cleared
    problem.EpochClock.ResolvePendingEpochs(() => { });

    Assert.Equal(1, problem.EpochClock.CurrentEpoch);

    // After clear, it must reevaluate
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(2, inner.Calls);
  }

  [Fact]
  public void HitStreakResets_WhenUncachedAppears() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000); // avoid natural boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    ) { GraceCount = 3 };

    // Prime cache
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // Two cached hits => hitCount=2
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // Uncached appears => tick increases and hitCount resets
    _ = cached.Evaluate([new DummyGenotype(2)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    // Another cached hit should not advance epoch (only 1 since reset)
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    Assert.Equal(0, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void EmptyBatch_DoesNothing() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000);
    var inner = new CountingEvaluator();

    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    ) { GraceCount = 1 };

    var epochBefore = problem.EpochClock.CurrentEpoch;
    var ticksBefore = problem.EpochClock.Ticks;

    var res = cached.Evaluate(Array.Empty<DummyGenotype>(), NoRandomGenerator.Instance, problem.SearchSpace, problem);

    Assert.Empty(res);
    Assert.Equal(0, inner.Calls);
    Assert.Equal(epochBefore, problem.EpochClock.CurrentEpoch);
    Assert.Equal(ticksBefore, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void AdvanceEpoch_JumpsTicksToNextEpochBoundary() {
    var env = new SystemRandomNumberGenerator(0);
    var problem = new DummyDynamicProblem(env, epochLength: 10_000);
    var inner = new CountingEvaluator();
    var cached = new DynamicCachedEvaluator<DummyGenotype, DummyEncoding, DummyDynamicProblem, int>(
      s => s.Value, inner, problem
    ) { GraceCount = 1 };

    // prime cache -> ticks=1
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // next fully cached batch triggers AdvanceEpoch immediately (GraceCount=1)
    _ = cached.Evaluate([new DummyGenotype(1)], NoRandomGenerator.Instance, problem.SearchSpace, problem);

    Assert.Equal(10_000L, problem.EpochClock.Ticks);
    Assert.Equal(0L, problem.EpochClock.Ticks % problem.EpochClock.EpochLength);
  }
}
