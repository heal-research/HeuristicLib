using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using MetaCandidate = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class DynamicRacingResolutionTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 3)]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    public async Task EpochChangeDuringMove_EndsTheStepAndEachStepCapturesAFreshBaseline(int burnInEpochs, int epochJump)
    {
        using var problem = new EpochProblem(epochLength: 10_000);
        problem.UpdateOnce();
        var moves = 0;
        var started = 0;
        var disposed = 0;
        var contender = new LifetimeProbeAlgorithm
        {
            BeforeMove = () =>
            {
                if (++moves % 2 != 0)
                    return;
                for (var update = 0; update < epochJump; update++)
                    problem.UpdateOnce();
            },
            Started = () => started++,
            Disposed = () => disposed++
        };
        var racing = new DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, LifetimeProbeAlgorithm>(
            new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
            new MetaCreator(), new MetaMutator(), new BestPopulationStateMerger<int>(),
            _ => contender, algorithm => algorithm.Evaluator)
        {
            NoRacers = 2,
            BurnInEpochs = burnInEpochs,
            EarlyTerminationStrength = 0,
            HallOfFameStrength = 0
        };
        await using var stream = Run(ResolutionScope.Create(), racing, problem).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await stream.MoveNextAsync()).ShouldBeTrue();
        moves.ShouldBe(2);
        problem.CurrentEpoch.ShouldBe(1 + epochJump);
        started.ShouldBe(burnInEpochs == 0 ? 2 : 1);
        disposed.ShouldBe(started);

        // A change while the outer stream is paused precedes the next step's baseline.
        problem.UpdateOnce();
        (await stream.MoveNextAsync()).ShouldBeTrue();
        moves.ShouldBe(4);
        problem.CurrentEpoch.ShouldBe(2 + 2 * epochJump);
        started.ShouldBe(burnInEpochs == 0 ? 4 : 3);
        disposed.ShouldBe(started);
    }

    // Every contender resolves its algorithm in a child scope of its own, so contenders racing one algorithm reference get
    // separate executors.
    [Fact]
    public async Task ContendersOfOneAlgorithm_EachBuildTheirOwnExecutor()
    {
        using var problem = new EpochProblem(epochLength: 10_000);
        problem.UpdateOnce();
        var moves = 0;
        var built = 0;
        var started = 0;
        var contender = new LifetimeProbeAlgorithm
        {
            // An epoch change on every second move ends each race step.
            BeforeMove = () =>
            {
                if (++moves % 2 == 0)
                    problem.UpdateOnce();
            },
            Built = () => built++,
            Started = () => started++,
            Disposed = static () => { }
        };
        var racing = new DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, LifetimeProbeAlgorithm>(
            new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
            new MetaCreator(), new MetaMutator(), new BestPopulationStateMerger<int>(),
            _ => contender, algorithm => algorithm.Evaluator)
        {
            NoRacers = 2,
            BurnInEpochs = 0,
            EarlyTerminationStrength = 0,
            HallOfFameStrength = 0
        };
        await using var stream = Run(ResolutionScope.Create(), racing, problem).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await stream.MoveNextAsync()).ShouldBeTrue();

        started.ShouldBe(2);
        built.ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task NewExecutorStartsIndependentBurnInWhilePausedExecutorKeepsProgress(int racers)
    {
        using var problem = new EpochProblem();
        var metaCreator = new MetaCreator();
        var metaMutator = new MetaMutator();
        var contender = new GeneticAlgorithm<int>
        {
            Creator = new CandidateCreator(),
            Crossover = SelectFirstParentCrossover<int>.Instance,
            Mutator = new CandidateMutator(),
            PopulationSize = 1,
            MaximumGenerations = 1
        };
        var sources = new List<GeneticAlgorithm<int>?>();
        var interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        var racing = new DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, GeneticAlgorithm<int>>(
            new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
            metaCreator, metaMutator, new BestPopulationStateMerger<int>(),
            (_, source) => { sources.Add(source); return contender; }, algorithm => algorithm.Evaluator)
        {
            NoRacers = racers,
            BurnInEpochs = 1,
            EarlyTerminationStrength = 0,
            HallOfFameStrength = 0,
            Interceptor = interceptor
        };
        var creations = new CountAccumulator();
        var mutations = new CountAccumulator();
        var parent = ResolutionScope.Create();
        parent.AddAspect(Aspect.Observe(metaCreator.Invocations, _ => creations.IncrementBy(1)));
        parent.AddAspect(Aspect.Observe(metaMutator.Invocations, _ => mutations.IncrementBy(1)));
        // Prebind the contender so its internal evaluator must be rebound for each performance observer.
        _ = parent.Resolve<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>(contender);
        await using var paused = Run(parent, racing, problem).GetAsyncEnumerator();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(1);
        mutations.CurrentCount.ShouldBe(0);
        sources[0].ShouldBeNull();

        var evaluations = new CountAccumulator();
        var interceptions = new CountAccumulator();
        var child = parent.CreateChildScope();
        child.AddAspect(Aspect.Observe(contender.Evaluator.Invocations, _ => evaluations.IncrementBy(1)));
        child.AddAspect(Aspect.Observe(interceptor.Invocations, _ => interceptions.IncrementBy(1)));
        await using var observed = Run(child, racing, problem).GetAsyncEnumerator();
        (await observed.MoveNextAsync()).ShouldBeTrue();
        sources[1].ShouldBeNull();
        creations.CurrentCount.ShouldBe(2);
        mutations.CurrentCount.ShouldBe(0);
        evaluations.CurrentCount.ShouldBeGreaterThan(0);
        interceptions.CurrentCount.ShouldBe(1);
        var observedEvaluations = evaluations.CurrentCount;

        (await paused.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(2);
        mutations.CurrentCount.ShouldBe(racers - 1);
        evaluations.CurrentCount.ShouldBe(observedEvaluations);
        interceptions.CurrentCount.ShouldBe(1);

        var independentSourceIndex = sources.Count;
        var independent = ResolutionScope.Create();
        independent.AddAspect(Aspect.Observe(metaCreator.Invocations, _ => creations.IncrementBy(1)));
        await using var fresh = Run(independent, racing, problem).GetAsyncEnumerator();
        (await fresh.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(3);
        sources[independentSourceIndex].ShouldBeNull();
        mutations.CurrentCount.ShouldBe(racers - 1);
    }

    // D3: a substitution stops at the delegate, so an outer one reports it, and whoever writes the delegate places the trace
    // inside it, where every contender then records into the run's one trace as an outer trace does through advice.
    [Fact]
    public async Task ATraceOfContenders_IsPlacedInTheBuilderAndRecordsWhatAnOuterTraceRecords()
    {
        var contender = new LifetimeProbeAlgorithm { Started = static () => { }, Disposed = static () => { } };
        var trace = contender.Evaluator.Invocations.Trace(_ => 1);

        var outer = Racing(_ => contender);
        Should.Throw<InvalidOperationException>(() => outer.Substitute(trace.Substitutions)).Message.ShouldContain("AlgBuilder");

        using var outerProblem = new EpochProblem(epochLength: 10_000);
        outerProblem.UpdateOnce();
        var withAdvice = ResolutionScope.Create();
        withAdvice.AddAspect(withAdvice.Results.Aspect(trace));
        await Steps(withAdvice, outer, outerProblem);

        using var innerProblem = new EpochProblem(epochLength: 10_000);
        innerProblem.UpdateOnce();
        var placed = ResolutionScope.Create();
        await Steps(placed, Racing(_ => contender.Substitute(trace.Substitutions)), innerProblem);

        withAdvice.Results.Result(trace).SampleCount.ShouldBeGreaterThan(0);
        placed.Results.Result(trace).SampleCount.ShouldBe(withAdvice.Results.Result(trace).SampleCount);

        static DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, LifetimeProbeAlgorithm> Racing(
            Func<MetaCandidate, LifetimeProbeAlgorithm> build) =>
            new(new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
                new MetaCreator(), new MetaMutator(), new BestPopulationStateMerger<int>(),
                (genotype, _) => build(genotype), algorithm => algorithm.Evaluator)
            {
                NoRacers = 2,
                BurnInEpochs = 0,
                EarlyTerminationStrength = 0,
                HallOfFameStrength = 0
            };

        static async Task Steps(ResolutionScope scope, IAlgorithm<int> racing, EpochProblem problem)
        {
            await using var stream = Run(scope, racing, problem).GetAsyncEnumerator(TestContext.Current.CancellationToken);
            for (var step = 0; step < 3; step++)
                (await stream.MoveNextAsync()).ShouldBeTrue();
        }
    }

    private static IAsyncEnumerable<PopulationState<int>> Run(ResolutionScope scope, IAlgorithm<int> algorithm, EpochProblem problem) =>
        scope.Resolve<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>(algorithm)
            .RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("step")]
    [InlineData("termination")]
    [InlineData("merge")]
    [InlineData("dispose")]
    [InlineData("merge-and-dispose")]
    public async Task FailedRace_DisposesEveryStartedContenderAndPreservesFailures(string phase)
    {
        var failure = new InvalidOperationException(phase);
        var objective = phase == "termination"
            ? new ObjectiveDirections([ObjectiveDirection.Minimize], Comparer<ObjectiveVector>.Create((_, _) => throw failure))
            : SingleObjective.Minimize;
        using var problem = new EpochProblem(objective);
        var started = 0;
        var disposed = 0;
        var evaluations = 0;
        var cleanupFailures = new List<Exception>();
        var contender = new LifetimeProbeAlgorithm
        {
            BeforeMove = () =>
            {
                if (phase == "step" && ++evaluations == 3)
                    throw failure;
            },
            Started = () => started++,
            Disposed = () =>
            {
                disposed++;
                if (phase is "dispose" or "merge-and-dispose")
                {
                    var cleanup = new InvalidOperationException($"cleanup {disposed}");
                    cleanupFailures.Add(cleanup);
                    throw cleanup;
                }
            }
        };
        var racing = new DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, LifetimeProbeAlgorithm>(
            new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
            new MetaCreator(), new MetaMutator(),
            new DelegatingRacingStateMerger<int, PopulationState<int>>((states, _) => phase is "merge" or "merge-and-dispose" ? throw failure : states[0]),
            _ => contender, algorithm => algorithm.Evaluator)
        {
            NoRacers = 2,
            EarlyTerminationStrength = phase == "termination" ? 0.1 : 0,
            HallOfFameStrength = 0
        };
        await using var stream = Run(ResolutionScope.Create(), racing, problem).GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var exception = await Should.ThrowAsync<Exception>(async () => await stream.MoveNextAsync());

        started.ShouldBe(2);
        disposed.ShouldBe(2);
        if (phase is "dispose" or "merge-and-dispose")
        {
            var aggregate = exception.ShouldBeOfType<AggregateException>();
            aggregate.InnerExceptions.ShouldBe(phase == "merge-and-dispose" ? [failure, .. cleanupFailures] : cleanupFailures);
        }
        else
            exception.ShouldBeSameAs(failure);
    }

    // The meta creator runs over the meta search space, so the racing run's types say nothing about it.
    [Fact]
    public void MetaOperators_AreAskedAboutTheMetaSearchSpaceRatherThanTheRun()
    {
        var execution = ExecutionSignature.For<UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>();
        var valid = Racing(new CreatorForMetaSpace());
        var invalid = Racing(new CreatorForAnotherMetaSpace());

        execution.Supports(valid).ShouldBeTrue();
        execution.Supports(invalid).ShouldBeFalse();
        var diagnostic = SearchConfigurationValidation.Validate(invalid, UnrestrictedSearchSpace<int>.Instance, execution).Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith(".Creator");
        diagnostic.Message.ShouldContain(nameof(CreatorForAnotherMetaSpace));

        static DynamicRacingAlgorithm<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>, LifetimeProbeAlgorithm> Racing(IOperator<Creation, MetaCandidate> creator) =>
            new(new MetaSpace(new BoundedRealVectorSearchSpace(1, 0, 1), new IntegerVectorSearchSpace(1, [0], [1])),
                creator, new MetaMutator(), new BestPopulationStateMerger<int>(),
                _ => new LifetimeProbeAlgorithm { BeforeMove = static () => { }, Started = static () => { }, Disposed = static () => { } }, algorithm => algorithm.Evaluator)
            {
                NoRacers = 2
            };
    }

    // The interceptor builds its evaluator without the run's search state, so the walk asks about what the resolve asks.
    [Fact]
    public void ReevaluationInterceptor_AsksItsEvaluatorAboutTheExecutionItResolves()
    {
        using var problem = new EpochProblem(epochLength: 10_000);
        var execution = ExecutionSignature.For<UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>();
        var valid = Interceptor(new EvaluatorUnder<ISearchState>());
        var invalid = Interceptor(new EvaluatorUnder<PopulationState<int>>());
        var scope = ResolutionScope.Create().For<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>();

        execution.Supports(valid).ShouldBeTrue();
        SearchConfigurationValidation.Validate(valid, UnrestrictedSearchSpace<int>.Instance, execution).IsValid.ShouldBeTrue();
        scope.Resolve(valid).ShouldNotBeNull();

        execution.Supports(invalid).ShouldBeFalse();
        var diagnostic = SearchConfigurationValidation.Validate(invalid, UnrestrictedSearchSpace<int>.Instance, execution).Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith(".Evaluator");
        Should.Throw<InvalidOperationException>(() => scope.Resolve(invalid)).Message.ShouldContain("EvaluatorUnder");

        ReevaluationInterceptor<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>> Interceptor(IOperator<Evaluation, int> evaluator) =>
            new(evaluator, problem);
    }

    // Written for one search state, as a consumer's evaluator can be.
    private sealed record EvaluatorUnder<TRequired> : Operator<Evaluation, int>
        where TRequired : class, ISearchState
    {
        public override bool Supports(ExecutionSignature execution) => execution.SearchState == typeof(TRequired);

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            scope.Resolve(new ProblemEvaluator<int>());
    }

    private sealed class AnotherMetaSpace : ISearchSpace<MetaCandidate>
    {
        public bool Contains(MetaCandidate candidate) => true;
    }

    private sealed record CreatorForMetaSpace : StatelessCreator<MetaCandidate, MetaSpace>
    {
        public override IReadOnlyList<MetaCandidate> Create(int count, IRandomNumberGenerator random, MetaSpace searchSpace) => new MetaCandidate[count];
    }

    private sealed record CreatorForAnotherMetaSpace : StatelessCreator<MetaCandidate, AnotherMetaSpace>
    {
        public override IReadOnlyList<MetaCandidate> Create(int count, IRandomNumberGenerator random, AnotherMetaSpace searchSpace) => new MetaCandidate[count];
    }

    private sealed record LifetimeProbeAlgorithm : Algorithm<LifetimeProbeAlgorithm, int, PopulationState<int>>
    {
        public IOperator<Evaluation, int> Evaluator { get; init; } = new ProblemEvaluator<int>();

        public static Parameter<LifetimeProbeAlgorithm, IOperator<Evaluation, int>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];

        public Action BeforeMove { get; init; } = static () => { };
        public Action Built { get; init; } = static () => { };
        public required Action Started { get; init; }
        public required Action Disposed { get; init; }

        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            Built();
            return new Executor<TSearchSpace, TProblem>(scope.Resolve(Evaluator), BeforeMove, Started, Disposed);
        }

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator, Action beforeMove, Action started, Action disposed)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                started();
                try
                {
                    for (var step = 0; step < 10; step++)
                    {
                        ct.ThrowIfCancellationRequested();
                        beforeMove();
                        int[] candidates = [0];
                        var qualities = evaluator.Evaluate(candidates, random, problem.SearchSpace, problem);
                        yield return Population.From(candidates.ToEvaluated(qualities)).ToPopulationState();
                        await Task.CompletedTask;
                    }
                }
                finally
                {
                    disposed();
                }
            }
        }
    }

    private sealed class EpochProblem(ObjectiveDirections? objective = null, int epochLength = 3) : DynamicProblem<EpochProblem, int, UnrestrictedSearchSpace<int>>(
        objective ?? SingleObjective.Minimize, UnrestrictedSearchSpace<int>.Instance, RandomNumberGenerator.Create(0),
        new EvaluationCountSchedule(epochLength), UpdatePolicy.AfterEachBatchEvaluation)
    {
        protected override ObjectiveVector Evaluate(int candidate, IRandomNumberGenerator random, int epoch) => new(candidate);
        protected override void Update() { }
    }

    private sealed record MetaCreator : StatelessCreator<MetaCandidate>
    {
        public override IReadOnlyList<MetaCandidate> Create(int count, IRandomNumberGenerator random) =>
            Enumerable.Range(0, count).Select(_ => new MetaCandidate(RealVector.Create([0.0]), IntegerVector.Create([0]))).ToArray();
    }

    private sealed record MetaMutator : SingleCandidateMutator<MetaCandidate>
    {
        public override MetaCandidate MutateCandidate(MetaCandidate parent, IRandomNumberGenerator random) => parent;
    }

    private sealed record CandidateCreator : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => Enumerable.Repeat(0, count).ToArray();
    }

    private sealed record CandidateMutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent;
    }
}
