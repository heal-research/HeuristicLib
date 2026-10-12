using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class MutatorSelectionTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Fact]
    public async Task RoleSelection_CountsTheConfiguredCompositeAndEachOfItsChildren()
    {
        var small = new StepMutator(1);
        var large = new StepMutator(5);
        var mutator = ChooseOneMutator.Create<int>(small, large);
        var counts = new Dictionary<IMutator<int>, CountAccumulator>(ReferenceEqualityComparer.Instance);
        var scope = ResolutionScope.Create(builder => builder.Wrap(NodeSelector.OfType<IMutator<int>>(), source =>
        {
            var count = new CountAccumulator();
            counts.Add(source, count);
            return source.CountCandidates(count);
        }));

        await RunToCompletion(scope, Algorithm(mutator));

        counts.Keys.ShouldBe([mutator, small, large], ignoreOrder: true);
        counts[mutator].CurrentCount.ShouldBePositive();
        (counts[small].CurrentCount + counts[large].CurrentCount).ShouldBe(counts[mutator].CurrentCount);
    }

    [Fact]
    public async Task RoleSelection_ReachesMutatorsOfAlgorithmsResolvedLaterByCycle()
    {
        var first = new StepMutator(1);
        var second = new StepMutator(2);
        var cycle = Algorithm(first).CycleWith(Algorithm(second), maximumCycles: 2);
        var activations = new List<(IMutator<int> Source, CountAccumulator Mutations)>();
        var scope = ResolutionScope.Create(builder => builder.Wrap(NodeSelector.OfType<IMutator<int>>(), source =>
        {
            var mutations = new CountAccumulator();
            activations.Add((source, mutations));
            return source.CountCandidates(mutations);
        }));

        await RunToCompletion(scope, cycle);

        activations.Select(activation => activation.Source).ShouldBe([first, second, first, second]);
        activations.ShouldAllBe(activation => activation.Mutations.CurrentCount > 0);
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(false, 0.35)]
    [InlineData(false, 1.0)]
    [InlineData(true, 0.0)]
    [InlineData(true, 0.35)]
    [InlineData(true, 1.0)]
    public async Task InternalMutationRate_MatchesExplicitComposition_WithTheSameSeed(bool nsga, double rate)
    {
        IMutator<int> source = new StepMutator(3);
        var ga = Algorithm(source) with { MutationRate = rate, PopulationSize = 16, MaximumGenerations = 5 };
        var nsga2 = new NSGA2<int>
        {
            Creator = ga.Creator,
            Crossover = ga.Crossover,
            Mutator = source,
            Selector = ga.Selector,
            Replacer = new ElitismReplacer<int>(elites: 0),
            PopulationSize = ga.PopulationSize,
            MaximumGenerations = ga.MaximumGenerations,
            MutationRate = rate
        };
        var explicitMutator = rate >= 1 ? source : source.AppliedAtRate(rate);
        IAlgorithm<int, PopulationState<int>> algorithm = nsga ? nsga2 : ga;
        IAlgorithm<int, PopulationState<int>> baseline = nsga
            ? nsga2 with { Mutator = explicitMutator, MutationRate = 1 }
            : ga with { Mutator = explicitMutator, MutationRate = 1 };

        var actual = await States(ResolutionScope.Create(), algorithm);
        var expected = await States(ResolutionScope.Create(), baseline);
        actual.Length.ShouldBe(expected.Length);
        for (var i = 0; i < actual.Length; i++)
            actual[i].Population.EvaluatedCandidates.ShouldBe(expected[i].Population.EvaluatedCandidates);
    }

    [Fact]
    public async Task MutationRate_OnlyOffersConfiguredSources_AndCountersSeeOnlyRoutedCandidates()
    {
        IMutator<int> source = new StepMutator(1);
        var offered = new List<IMutator<int>>();
        var count = new CountAccumulator();
        var scope = ResolutionScope.Create(builder => builder.Wrap(NodeSelector.OfType<IMutator<int>>().And(mutator =>
        {
            offered.Add(mutator);
            return true;
        }), mutator => mutator.CountCandidates(count)));
        var algorithm = Algorithm(source) with { PopulationSize = 16, MaximumGenerations = 2, MutationRate = 0.5 };

        var states = await States(scope, algorithm);
        offered.ShouldBe([source]);
        var mutated = states[1].Population.EvaluatedCandidates.Count(candidate => candidate.Candidate == 99);
        mutated.ShouldBeInRange(1, 15);
        count.CurrentCount.ShouldBe(mutated);
        // Another caller reaches the same source without passing through the algorithm's rate composition.
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>().Resolve(source)
            .Mutate([100], RandomNumberGenerator.Create(7), Problem.SearchSpace, Problem).ShouldBe([99]);
        count.CurrentCount.ShouldBe(mutated + 1);
    }

    [Fact]
    public async Task PausedAlgorithm_KeepsItsComposition_WhenAnotherContextAddsASourceObserver()
    {
        IMutator<int> source = new StepMutator(1);
        var algorithm = Algorithm(source) with { PopulationSize = 16, MaximumGenerations = 3, MutationRate = 0.5 };
        var parent = ResolutionScope.Create();
        await using var paused = Run(parent, algorithm).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observed = new CountAccumulator();
        var child = parent.CreateChildScope(builder => builder.Wrap(source, mutator => mutator.CountCandidates(observed)));
        await using var rebound = Run(child, algorithm).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await rebound.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        observed.CurrentCount.ShouldBe(0);
        (await rebound.MoveNextAsync()).ShouldBeTrue();
        observed.CurrentCount.ShouldBePositive();
        var counted = observed.CurrentCount;
        (await paused.MoveNextAsync()).ShouldBeTrue();
        observed.CurrentCount.ShouldBe(counted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ZeroMutationRate_DoesNotObserveTheConfiguredSource_EvenWhenItIsTheSharedNoChangeMutator(bool noChange)
    {
        IMutator<int> source = noChange ? NoChangeMutator<int>.Instance : new StepMutator(1);
        var count = new CountAccumulator();
        var scope = ResolutionScope.Create(builder => builder.Wrap(source, mutator => mutator.CountCandidates(count)));
        await RunToCompletion(scope, Algorithm(source) with { MutationRate = 0 });
        count.CurrentCount.ShouldBe(0);
    }

    private static GeneticAlgorithm<int> Algorithm(IMutator<int> mutator) => new()
    {
        Creator = new ConstantCreator(),
        Crossover = SelectFirstParentCrossover<int>.Instance,
        Mutator = mutator,
        Selector = new RandomSelector<int>(),
        PopulationSize = 4,
        Elites = 0,
        MaximumGenerations = 3,
        MutationRate = 0.5
    };

    private static IAsyncEnumerable<PopulationState<int>> Run(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm)
            .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken);

    private static ValueTask<PopulationState<int>[]> States(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        Run(scope, algorithm).ToArrayAsync(TestContext.Current.CancellationToken);

    private static async Task RunToCompletion(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm)
    {
        var execution = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);
        await execution.RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken)
            .LastAsync(TestContext.Current.CancellationToken);
    }

    private sealed record ConstantCreator : SingleCandidateCreator<int>
    {
        public override int CreateCandidate(IRandomNumberGenerator random) => 100;
    }

    private sealed record StepMutator(int Step) : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent - Step;
    }
}
