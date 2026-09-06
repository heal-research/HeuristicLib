using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic;

//candidates must be class types
file sealed class DummyGenotype(int val)
{
    public readonly int Value = val;
}

file sealed class DummySearchSpace : ISearchSpace<DummyGenotype>
{
    public bool Contains(DummyGenotype candidate) => true;
}

file sealed class DummyDynamicProblem : DynamicProblem<DummyGenotype, DummySearchSpace>
{
    public DummyDynamicProblem(IRandomNumberGenerator env, int epochLength)
      : base(SingleObjective.Minimize, new DummySearchSpace(), env, new EvaluationCountSchedule(epochLength), UpdatePolicy.AfterEachBatchEvaluation)
    { }

    /// <summary>How many candidates this problem was asked to score.</summary>
    public long Evaluations { get; private set; }

    protected override ObjectiveVector Evaluate(DummyGenotype solution, IRandomNumberGenerator random, int epoch)
    {
        Evaluations++;
        return solution.Value;
    }

    protected override void Update() { }
}

file sealed record CountingEvaluator : StatelessEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem>
{
    public int Calls { get; private set; }
    public int LastBatchSize { get; private set; }
    public IRandomNumberGenerator? LastRandom { get; private set; }

    public override IReadOnlyList<ObjectiveVector> Evaluate(
      IReadOnlyList<DummyGenotype> solutions,
      IRandomNumberGenerator random,
      DummySearchSpace searchSpace,
      DummyDynamicProblem problem)
    {
        Calls++;
        LastBatchSize = solutions.Count;
        LastRandom = random;

        return problem.Evaluate(solutions, random);
    }
}

file sealed record DummyGenotypeValueCacheKeySelector : ICacheKeySelector<DummyGenotype, int>
{
    public static DummyGenotypeValueCacheKeySelector Instance { get; } = new();

    public int SelectKey(DummyGenotype candidate) => candidate.Value;
}

public class DynamicEvaluationCacheTests
{
    [Fact]
    public void ReevaluationInterceptor_UsesProvidedIterationRandomAfterEpochChange()
    {
        var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var interceptor = new ReevaluationInterceptor<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(evaluator, problem);
        var instance = ExecutionInstanceResolver.Create().Resolve(interceptor);
        var candidate = new DummyGenotype(1);
        var state = Population.From([EvaluatedCandidate.From(candidate, new ObjectiveVector(99.0))]).ToPopulationState();
        var random = RandomNumberGenerator.Create(1);

        problem.UpdateOnce();
        var result = instance.Transform(state, previousState: null, random, problem.SearchSpace, problem);

        evaluator.LastRandom.ShouldBeSameAs(random);
        result.Population.Single().ObjectiveVector.ShouldBe(new ObjectiveVector(1.0));
    }

    [Fact]
    public void DeduplicatesWithinBatch_EvaluatesOnce()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid boundary
        var inner = new CountingEvaluator();

        var cached = inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance);

        var res = ExecutionInstanceResolver.Create().Resolve(cached).Evaluate(
          [new DummyGenotype(1), new DummyGenotype(1), new DummyGenotype(1)],
          TestRandoms.NoRandom, problem.SearchSpace, problem);

        inner.Calls.ShouldBe(1);
        inner.LastBatchSize.ShouldBe(1);
        res.Select(v => v[0]).ToArray().ShouldBe([1.0, 1.0, 1.0]);

        // Only one real evaluation => one tick
        problem.Evaluations.ShouldBe(1L);
        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(0); // nothing was pending, so nothing advanced
    }

    [Fact]
    public void CachesAcrossBatches_NoReevaluation_NoTick()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid boundary
        var inner = new CountingEvaluator();

        var cached = ExecutionInstanceResolver.Create().Resolve(inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance));

        _ = cached.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(1);
        problem.Evaluations.ShouldBe(2L);

        // Fully cached => inner evaluator not called => no ticking
        _ = cached.Evaluate([new DummyGenotype(2), new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(1);
        problem.Evaluations.ShouldBe(2L);
    }

    [Fact]
    public void EpochBoundary_ClearsCache_ThenRequiresReevaluation()
    {
        var env = RandomNumberGenerator.Create(0);

        var problem = new DummyDynamicProblem(env, 2); // boundary every 2 evals
        var inner = new CountingEvaluator();

        var cached = inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance);
        var cachedInstance = ExecutionInstanceResolver.Create().Resolve(cached);

        // Evaluate two distinct keys -> 2 evaluations -> should hit boundary and schedule an epoch change
        _ = cachedInstance.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(1);
        problem.Evaluations.ShouldBe(2L);

        // resolve -> fires OnEpochChange -> cached evaluator clears cache
        problem.ApplyPendingUpdates();

        problem.CurrentEpoch.ShouldBe(1); // an epoch was pending, so applying it advanced the environment

        // Previously cached: now must be reevaluated
        _ = cachedInstance.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(2);
    }

    [Fact]
    public void GraceCount_Reached_AdvancesEpoch_WithoutTicking()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid natural boundary
        var inner = new CountingEvaluator();

        var cached = ExecutionInstanceResolver.Create().Resolve(inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 });

        // Prime cache (causes 1 tick)
        var dummyGenotype = new DummyGenotype(1);
        _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(1);
        problem.Evaluations.ShouldBe(1L);
        problem.CurrentEpoch.ShouldBe(0);

        // Now do 3 cached batches => no ticking, but should force AdvanceEpoch
        _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(1L); // still 1 => proves no ticking on cache hit
        _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(1L); // still 1 => proves no ticking on cache hit
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);

        problem.Evaluations.ShouldBe(1L); // the cached batches evaluated nothing

        // resolve -> epoch changes -> cache cleared
        problem.ApplyPendingUpdates();

        problem.CurrentEpoch.ShouldBe(1);

        // After clear, it must reevaluate
        _ = cached.Evaluate([dummyGenotype], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(2);
    }

    [Fact]
    public void HitStreakResets_WhenUncachedAppears()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid natural boundary
        var inner = new CountingEvaluator();

        var cached = ExecutionInstanceResolver.Create().Resolve(inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 });

        // Prime cache
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(1L);

        // Two cached hits => hitCount=2
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(1L);

        // Uncached appears => tick increases and hitCount resets
        _ = cached.Evaluate([new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(2L);

        // Another cached hit should not advance epoch (only 1 since reset)
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(2L);

        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(0); // nothing was pending, so nothing advanced
    }

    [Fact]
    public void EmptyBatch_DoesNothing()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000);
        var inner = new CountingEvaluator();

        var cached = ExecutionInstanceResolver.Create().Resolve(inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 1 });

        var epochBefore = problem.CurrentEpoch;
        var evaluationsBefore = problem.Evaluations;

        var res = cached.Evaluate([], TestRandoms.NoRandom, problem.SearchSpace, problem);

        res.ShouldBeEmpty();
        inner.Calls.ShouldBe(0);
        problem.Evaluations.ShouldBe(evaluationsBefore);

        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(epochBefore); // nothing was pending, so nothing advanced
    }

    [Fact]
    public void AdvanceEpoch_JumpsTicksToNextEpochBoundary()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000);
        var inner = new CountingEvaluator();
        var cached = ExecutionInstanceResolver.Create().Resolve(inner.WithCache(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 1 });

        // prime cache -> ticks=1
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.Evaluations.ShouldBe(1L);

        // next fully cached batch triggers AdvanceEpoch immediately (GraceCount=1)
        _ = cached.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);

        problem.Evaluations.ShouldBe(1L); // the cached batch evaluated nothing

        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(1); // and forced exactly one epoch
    }
}
