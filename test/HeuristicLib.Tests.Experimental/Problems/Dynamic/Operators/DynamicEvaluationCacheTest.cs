using System.Runtime.CompilerServices;
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

file sealed class DummyDynamicProblem : DynamicProblem<DummyDynamicProblem, DummyGenotype, DummySearchSpace>
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
    public void GraceCount_CountsRepeatedCachedCandidatesAndStartsFreshAfterEpochChange()
    {
        using var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var source = evaluator.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 };
        var execution = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);
        var candidate = new DummyGenotype(1);

        for (var epoch = 0; epoch < 2; epoch++)
        {
            execution.Evaluate([candidate], TestRandoms.NoRandom, problem.SearchSpace, problem);
            execution.Evaluate([candidate, candidate], TestRandoms.NoRandom, problem.SearchSpace, problem);
            problem.ApplyPendingUpdates();
            problem.CurrentEpoch.ShouldBe(epoch);

            execution.Evaluate([candidate], TestRandoms.NoRandom, problem.SearchSpace, problem);
            evaluator.Calls.ShouldBe(epoch + 1);
            problem.Evaluations.ShouldBe(epoch + 1L);
            problem.ApplyPendingUpdates();
            problem.CurrentEpoch.ShouldBe(epoch + 1);
        }
    }

    [Fact]
    public void GraceCount_MixedBatchWithRepeatedMissesResetsHitStreak()
    {
        using var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var source = evaluator.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 };
        var execution = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);
        var cached = new DummyGenotype(1);
        var uncached = new DummyGenotype(2);
        execution.Evaluate([cached], TestRandoms.NoRandom, problem.SearchSpace, problem);
        execution.Evaluate([cached, cached], TestRandoms.NoRandom, problem.SearchSpace, problem);

        var results = execution.Evaluate([cached, uncached, uncached], TestRandoms.NoRandom, problem.SearchSpace, problem);
        results.Select(result => result[0]).ShouldBe([1.0, 2.0, 2.0]);
        evaluator.Calls.ShouldBe(2);
        evaluator.LastBatchSize.ShouldBe(1);

        execution.Evaluate([cached, uncached], TestRandoms.NoRandom, problem.SearchSpace, problem);
        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(0);
        execution.Evaluate([uncached], TestRandoms.NoRandom, problem.SearchSpace, problem);
        evaluator.Calls.ShouldBe(2);
        problem.Evaluations.ShouldBe(2L);
        problem.ApplyPendingUpdates();
        problem.CurrentEpoch.ShouldBe(1);
    }

    [Fact]
    public void CacheRebinding_SharesEntriesAndKeepsChildObservationContexts()
    {
        using var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var source = evaluator.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance);
        var root = ResolutionScope.Create();
        var outer = root.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);
        var calls = new CountAccumulator();
        var child = root.CreateChildScope(builder => builder.Wrap<IEvaluator<DummyGenotype>>(evaluator, original => original.CountCalls(calls)));
        var inner = child.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);
        inner.ShouldNotBeSameAs(outer);

        outer.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        evaluator.Calls.ShouldBe(1);
        calls.CurrentCount.ShouldBe(0);
        inner.Evaluate([new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        outer.Evaluate([new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        evaluator.Calls.ShouldBe(2);
        calls.CurrentCount.ShouldBe(1);

        ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source)
            .Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        evaluator.Calls.ShouldBe(3);
        problem.UpdateOnce();
        inner.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        outer.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        evaluator.Calls.ShouldBe(4);
        calls.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void RelativeQualityRebinding_SharesTheBestKnownReferencePerEpoch()
    {
        using var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var references = 0;
        var provider = new FuncBestKnownObjectiveProvider<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(_ => { references++; return new ObjectiveVector(2); });
        var source = new DynamicRelativeQualityEvaluator<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(evaluator, problem, provider);
        var root = ResolutionScope.Create();
        var outer = root.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);
        var calls = new CountAccumulator();
        var child = root.CreateChildScope(builder => builder.Wrap<IEvaluator<DummyGenotype>>(evaluator, original => original.CountCalls(calls)));
        var inner = child.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source);

        outer.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        references.ShouldBe(1);
        ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(source)
            .Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        references.ShouldBe(2);
        problem.UpdateOnce();
        inner.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        outer.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        references.ShouldBe(3);
        calls.CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void ReevaluationRebinding_SharesPendingRequestsAndIsolatesIndependentRoots()
    {
        using var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var source = new ReevaluationInterceptor<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(evaluator, problem);
        var root = ResolutionScope.Create();
        var outer = root.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(source);
        var independent = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(source);
        var calls = new CountAccumulator();
        var child = root.CreateChildScope(builder => builder.Wrap<IEvaluator<DummyGenotype>>(evaluator, original => original.CountCalls(calls)));
        var inner = child.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(source);
        var state = Population.From([EvaluatedCandidate.From(new DummyGenotype(1), new ObjectiveVector(99))]).ToPopulationState();
        inner.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem).ShouldBeSameAs(state);

        problem.UpdateOnce();
        inner.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem).Population.Single().ObjectiveVector.ShouldBe(new ObjectiveVector(1));
        outer.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem).ShouldBeSameAs(state);
        independent.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem).Population.Single().ObjectiveVector.ShouldBe(new ObjectiveVector(1));
        evaluator.Calls.ShouldBe(2);
        calls.CurrentCount.ShouldBe(1);
        problem.UpdateOnce();
        outer.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Transform(state, null, TestRandoms.NoRandom, problem.SearchSpace, problem).ShouldBeSameAs(state);
        evaluator.Calls.ShouldBe(3);
        calls.CurrentCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EpochSubscription_DoesNotRetainExecutionOrChild(bool useReevaluationInterceptor)
    {
        var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var (execution, child) = CreateWeakExecutionReferences(problem, useReevaluationInterceptor);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        execution.IsAlive.ShouldBeFalse();
        child.IsAlive.ShouldBeFalse();
        problem.UpdateOnce();
        GC.KeepAlive(problem);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static (WeakReference Execution, WeakReference Child) CreateWeakExecutionReferences(DummyDynamicProblem problem, bool useReevaluationInterceptor)
        {
            var child = new CountingEvaluator();
            var scope = ResolutionScope.Create();
            object execution = useReevaluationInterceptor
                ? scope.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(
                    new ReevaluationInterceptor<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(child, problem))
                : scope.Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(child.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance));

            return (new WeakReference(execution), new WeakReference(child));
        }
    }

    [Fact]
    public void ReevaluationInterceptor_UsesProvidedIterationRandomAfterEpochChange()
    {
        var problem = new DummyDynamicProblem(RandomNumberGenerator.Create(0), 10_000);
        var evaluator = new CountingEvaluator();
        var interceptor = new ReevaluationInterceptor<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(evaluator, problem);
        var execution = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem, PopulationState<DummyGenotype>>(interceptor);
        var candidate = new DummyGenotype(1);
        var state = Population.From([EvaluatedCandidate.From(candidate, new ObjectiveVector(99.0))]).ToPopulationState();
        var random = RandomNumberGenerator.Create(1);

        problem.UpdateOnce();
        var result = execution.Transform(state, previousState: null, random, problem.SearchSpace, problem);

        evaluator.LastRandom.ShouldBeSameAs(random);
        result.Population.Single().ObjectiveVector.ShouldBe(new ObjectiveVector(1.0));
    }

    [Fact]
    public void DeduplicatesWithinBatch_EvaluatesOnce()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid boundary
        var inner = new CountingEvaluator();

        var cached = inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance);

        var res = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(cached).Evaluate(
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

        var cached = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance));

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

        var cached = inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance);
        var cachedExecution = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(cached);

        // Evaluate two distinct keys -> 2 evaluations -> should hit boundary and schedule an epoch change
        _ = cachedExecution.Evaluate([new DummyGenotype(1), new DummyGenotype(2)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(1);
        problem.Evaluations.ShouldBe(2L);

        // resolve -> fires OnEpochChange -> cached evaluator clears cache
        problem.ApplyPendingUpdates();

        problem.CurrentEpoch.ShouldBe(1); // an epoch was pending, so applying it advanced the environment

        // Previously cached: now must be reevaluated
        _ = cachedExecution.Evaluate([new DummyGenotype(1)], TestRandoms.NoRandom, problem.SearchSpace, problem);
        inner.Calls.ShouldBe(2);
    }

    [Fact]
    public void GraceCount_Reached_AdvancesEpoch_WithoutTicking()
    {
        var env = RandomNumberGenerator.Create(0);
        var problem = new DummyDynamicProblem(env, 10_000); // avoid natural boundary
        var inner = new CountingEvaluator();

        var cached = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 });

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

        var cached = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 3 });

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

        var cached = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 1 });

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
        var cached = ResolutionScope.Create().Resolve<DummyGenotype, DummySearchSpace, DummyDynamicProblem>(inner.Cached(problem, DummyGenotypeValueCacheKeySelector.Instance) with { GraceCount = 1 });

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
