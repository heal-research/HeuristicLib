using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests;

namespace HEAL.HeuristicLib.Extensions.Tests.Dynamic;

//genotypes must be class types
file sealed class DummyGenotype(int val)
{
  public readonly int Value = val;
}

file sealed class DummySearchSpace : ISearchSpace<DummyGenotype>
{
  public bool Contains(DummyGenotype genotype) => true;
}

file sealed class DummyDynamicProblem : DynamicProblem<DummyGenotype, DummySearchSpace>
{
  public DummyDynamicProblem(IRandomNumberGenerator env, int epochLength)
    : base(SingleObjective.Minimize, new DummySearchSpace(), env, UpdatePolicy.AfterEvaluation, epochLength)
  { }

  public override ObjectiveVector Evaluate(DummyGenotype solution, IRandomNumberGenerator random, EvaluationTiming timing) => solution.Value;

  protected override void Update() { }
}

file sealed record CountingEvaluator : StatelessEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem>
{
  public int Calls { get; private set; }
  public int LastBatchSize { get; private set; }

  public override IReadOnlyList<ObjectiveVector> Evaluate(
    IReadOnlyList<DummyGenotype> solutions,
    IRandomNumberGenerator random,
    DummySearchSpace searchSpace,
    DummyDynamicProblem problem)
  {
    Calls++;
    LastBatchSize = solutions.Count;

    return solutions.Select(s => problem.Evaluate(s, random)).ToArray();
  }
}

public class DynamicEvaluationCacheTests
{
  [Fact]
  public void DeduplicatesWithinBatch_EvaluatesOnce()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000); // avoid boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    );

    var res = cached.CreateExecutionInstance(TestRun.Instance).Evaluate(
      [new DummyGenotype(1), new DummyGenotype(1), new DummyGenotype(1)],
      TestRandoms.NoRandom, problem.SearchSpace, problem);

    Assert.Equal(1, inner.Calls);
    Assert.Equal(1, inner.LastBatchSize);
    Assert.Equal([1.0, 1.0, 1.0], res.Select(v => v[0]).ToArray());

    // Only one real evaluation => one tick
    Assert.Equal(1L, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void CachesAcrossBatches_NoReevaluation_NoTick()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000); // avoid boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    ).CreateExecutionInstance(TestRun.Instance);

    _ = cached.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    // Fully cached => inner evaluator not called => no ticking
    _ = cached.Evaluate([new DummyGenotype(2), new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);
  }

  [Fact]
  public void EpochBoundary_ClearsCache_ThenRequiresReevaluation()
  {
    var env = RandomNumberGenerator.Create(0);

    var problem = new DummyDynamicProblem(env, 2); // boundary every 2 evals
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    );

    // Evaluate two distinct keys -> 2 evaluations -> should hit boundary and schedule an epoch change
    _ = cached.CreateExecutionInstance(TestRun.Instance).Evaluate([new DummyGenotype(1), new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(2L, problem.EpochClock.Ticks);
    Assert.True(problem.EpochClock.PendingEpochs > 0);

    // resolve -> fires OnEpochChange -> cached evaluator clears cache
    problem.EpochClock.ResolvePendingEpochs(() => { });

    Assert.Equal(1, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);

    // Previously cached: now must be reevaluated
    _ = cached.CreateExecutionInstance(TestRun.Instance).Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(2, inner.Calls);
  }

  [Fact]
  public void GraceCount_Reached_AdvancesEpoch_WithoutTicking()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000); // avoid natural boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    ) { GraceCount = 3 }.CreateExecutionInstance(TestRun.Instance);

    // Prime cache (causes 1 tick)
    var dummyGenotype = new DummyGenotype(1);
    _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1, inner.Calls);
    Assert.Equal(1L, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.CurrentEpoch);

    // Now do 3 cached batches => no ticking, but should force AdvanceEpoch
    _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks); // still 1 => proves no ticking on cache hit
    _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks); // still 1 => proves no ticking on cache hit
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);

    Assert.Equal(10_000L, problem.EpochClock.Ticks);
    Assert.True(problem.EpochClock.PendingEpochs > 0); // epoch forced

    // resolve -> epoch changes -> cache cleared
    problem.EpochClock.ResolvePendingEpochs(() => { });

    Assert.Equal(1, problem.EpochClock.CurrentEpoch);

    // After clear, it must reevaluate
    _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(2, inner.Calls);
  }

  [Fact]
  public void HitStreakResets_WhenUncachedAppears()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000); // avoid natural boundary
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    ) { GraceCount = 3 }.CreateExecutionInstance(TestRun.Instance);

    // Prime cache
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // Two cached hits => hitCount=2
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // Uncached appears => tick increases and hitCount resets
    _ = cached.Evaluate([new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    // Another cached hit should not advance epoch (only 1 since reset)
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(2L, problem.EpochClock.Ticks);

    Assert.Equal(0, problem.EpochClock.CurrentEpoch);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void EmptyBatch_DoesNothing()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000);
    var inner = new CountingEvaluator();

    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    ) { GraceCount = 1 }.CreateExecutionInstance(TestRun.Instance);

    var epochBefore = problem.EpochClock.CurrentEpoch;
    var ticksBefore = problem.EpochClock.Ticks;

    var res = cached.Evaluate([], TestRandoms.NoRandom, problem.SearchSpace, problem);

    Assert.Empty(res);
    Assert.Equal(0, inner.Calls);
    Assert.Equal(epochBefore, problem.EpochClock.CurrentEpoch);
    Assert.Equal(ticksBefore, problem.EpochClock.Ticks);
    Assert.Equal(0, problem.EpochClock.PendingEpochs);
  }

  [Fact]
  public void AdvanceEpoch_JumpsTicksToNextEpochBoundary()
  {
    var env = RandomNumberGenerator.Create(0);
    var problem = new DummyDynamicProblem(env, 10_000);
    var inner = new CountingEvaluator();
    var cached = new DynamicCachingEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem, int>(
      inner, problem, keySelector: s => s.Value
    ) { GraceCount = 1 }.CreateExecutionInstance(TestRun.Instance);

    // prime cache -> ticks=1
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
    Assert.Equal(1L, problem.EpochClock.Ticks);

    // next fully cached batch triggers AdvanceEpoch immediately (GraceCount=1)
    _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);

    Assert.Equal(10_000L, problem.EpochClock.Ticks);
    Assert.Equal(0L, problem.EpochClock.Ticks % problem.EpochClock.EpochLength);
  }
}
