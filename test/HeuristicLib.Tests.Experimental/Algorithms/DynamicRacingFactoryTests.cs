using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using MetaCandidate = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class DynamicRacingFactoryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Rebinding_PreservesIncumbentAndBurnInProgressAndObservesDeferredContenders(int racers)
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
        var parent = ResolutionScope.Create(builder =>
        {
            builder.Wrap<ICreator<MetaCandidate>>(metaCreator, original => original.CountCalls(creations));
            builder.Wrap<IMutator<MetaCandidate>>(metaMutator, original => original.CountCalls(mutations));
        });
        // Prebind the contender so its internal evaluator must be rebound for each performance observer.
        _ = parent.Resolve<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>(contender);
        await using var paused = Run(parent, racing, problem).GetAsyncEnumerator();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(1);
        mutations.CurrentCount.ShouldBe(0);
        sources[0].ShouldBeNull();

        var evaluations = new CountAccumulator();
        var interceptions = new CountAccumulator();
        var child = parent.CreateChildScope(builder =>
        {
            builder.Wrap(contender.Evaluator, original => original.CountCalls(evaluations));
            builder.Wrap<IInterceptor<int>>(interceptor, original => original.CountCalls(interceptions));
        });
        await using var observed = Run(child, racing, problem).GetAsyncEnumerator();
        (await observed.MoveNextAsync()).ShouldBeTrue();
        sources[1].ShouldBeSameAs(contender);
        creations.CurrentCount.ShouldBe(1);
        mutations.CurrentCount.ShouldBe(racers - 1);
        evaluations.CurrentCount.ShouldBeGreaterThan(0);
        interceptions.CurrentCount.ShouldBe(1);
        var observedEvaluations = evaluations.CurrentCount;

        (await paused.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(1);
        mutations.CurrentCount.ShouldBe(2 * (racers - 1));
        evaluations.CurrentCount.ShouldBe(observedEvaluations);
        interceptions.CurrentCount.ShouldBe(1);

        var independentSourceIndex = sources.Count;
        var independent = ResolutionScope.Create(builder =>
            builder.Wrap<ICreator<MetaCandidate>>(metaCreator, original => original.CountCalls(creations)));
        await using var fresh = Run(independent, racing, problem).GetAsyncEnumerator();
        (await fresh.MoveNextAsync()).ShouldBeTrue();
        creations.CurrentCount.ShouldBe(2);
        sources[independentSourceIndex].ShouldBeNull();
        mutations.CurrentCount.ShouldBe(2 * (racers - 1));
    }

    private static IAsyncEnumerable<PopulationState<int>> Run(ResolutionScope scope, IAlgorithm<int> algorithm, EpochProblem problem) =>
        scope.Resolve<int, UnrestrictedSearchSpace<int>, EpochProblem, PopulationState<int>>(algorithm)
            .RunStreamingAsync(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken);

    private sealed class EpochProblem() : DynamicProblem<EpochProblem, int, UnrestrictedSearchSpace<int>>(
        SingleObjective.Minimize, UnrestrictedSearchSpace<int>.Instance, RandomNumberGenerator.Create(0),
        new EvaluationCountSchedule(3), UpdatePolicy.AfterEachBatchEvaluation)
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
