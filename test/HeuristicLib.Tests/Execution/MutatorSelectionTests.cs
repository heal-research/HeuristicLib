using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
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
        var mutator = ChoosePerItem.Create<Mutation, int>(small, large);
        var selected = new List<(IOperator<Mutation, int> Mutator, Counter Count)>();
        var counted = Algorithm(mutator).Substitute((IOperator<Mutation, int> selection) => Counting(selection, selected));
        var scope = ResolutionScope.Create();

        await RunToCompletion(scope, counted);

        // The children are selected first; the composite selected last already holds their counted copies.
        selected.Count.ShouldBe(3);
        selected.Take(2).Select(entry => entry.Mutator).ShouldBe([small, large]);
        var counts = selected.Select(entry => scope.Results.Result(entry.Count).CurrentCount).ToArray();
        counts[2].ShouldBePositive();
        (counts[0] + counts[1]).ShouldBe(counts[2]);
    }

    [Fact]
    public async Task RoleSelection_ReachesTheMutatorsOfTheAlgorithmsACycleHolds()
    {
        var first = new StepMutator(1);
        var second = new StepMutator(2);
        var cycle = Algorithm(first).CycleWith(Algorithm(second), maximumCycles: 2);
        var selected = new List<(IOperator<Mutation, int> Mutator, Counter Count)>();
        var counted = cycle.Substitute((IOperator<Mutation, int> selection) => Counting(selection, selected));
        var scope = ResolutionScope.Create();

        await RunToCompletion(scope, counted);

        selected.Select(selection => selection.Mutator).ShouldBe([first, second]);
        selected.ShouldAllBe(selection => scope.Results.Result(selection.Count).CurrentCount > 0);
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(false, 0.35)]
    [InlineData(false, 0.5)]
    [InlineData(false, -0.25)]
    [InlineData(false, double.NaN)]
    [InlineData(false, double.NegativeInfinity)]
    [InlineData(false, 1.0)]
    [InlineData(false, 1.5)]
    [InlineData(false, double.PositiveInfinity)]
    [InlineData(true, 0.0)]
    [InlineData(true, 0.35)]
    [InlineData(true, 0.5)]
    [InlineData(true, -0.25)]
    [InlineData(true, double.NaN)]
    [InlineData(true, double.NegativeInfinity)]
    [InlineData(true, 1.0)]
    [InlineData(true, 1.5)]
    [InlineData(true, double.PositiveInfinity)]
    public async Task MutationRate_MatchesExplicitAppliedAtRate_WithTheSameSeed(bool nsga, double rate)
    {
        IOperator<Mutation, int> source = new RandomStepMutator();
        var ga = Algorithm(source) with { MutationRate = rate, PopulationSize = 16, MaximumGenerations = 5, Evaluator = new RandomEvaluator() };
        var nsga2 = AsNsga2(ga);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MutationRate_OnlyOffersConfiguredSources_AndCountersSeeOnlyRoutedCandidates(bool nsga)
    {
        IOperator<Mutation, int> source = new StepMutator(1);
        var offered = new List<IOperator<Mutation, int>>();
        var counter = new Counter();
        var countedSource = source.Counted(counter, OperatorCountMetric.Candidates);
        var scope = ResolutionScope.Create();
        var count = scope.Results.Result(counter);
        var algorithm = Algorithm(source) with { PopulationSize = 16, MaximumGenerations = 2, MutationRate = 0.5 };
        IAlgorithm<int, PopulationState<int>> root = nsga ? AsNsga2(algorithm) : algorithm;
        var counted = root.Substitute(Substitution.Of<IOperator<Mutation, int>>(mutator =>
        {
            offered.Add(mutator);
            return true;
        }, _ => countedSource));

        var states = await States(scope, counted);
        offered.ShouldBe([source]);
        var mutated = states[1].Population.EvaluatedCandidates.Count(candidate => candidate.Candidate == 99);
        mutated.ShouldBeInRange(1, 15);
        count.CurrentCount.ShouldBe(mutated);
        // Another caller reaches the same source without the algorithm's mutation rate.
        scope.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Problem).Resolve(countedSource)
            .Mutate([100], RandomNumberGenerator.Create(7)).ShouldBe([99]);
        count.CurrentCount.ShouldBe(mutated + 1);
    }

    [Fact]
    public async Task PausedAlgorithm_KeepsItsMutator_WhenAnotherContextAddsASourceObserver()
    {
        IOperator<Mutation, int> source = new StepMutator(1);
        var algorithm = Algorithm(source) with { PopulationSize = 16, MaximumGenerations = 3, MutationRate = 0.5 };
        var parent = ResolutionScope.Create();
        await using var paused = Run(parent, algorithm).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observing = algorithm.Counting(source, out var counter, OperatorCountMetric.Candidates);
        var observed = parent.Results.Result(counter);
        var child = parent.CreateChildScope();
        await using var rebound = Run(child, observing).GetAsyncEnumerator(TestContext.Current.CancellationToken);
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
    [InlineData(false, false, 0.0)]
    [InlineData(false, true, 0.0)]
    [InlineData(true, false, 0.0)]
    [InlineData(true, true, 0.0)]
    [InlineData(false, false, -0.25)]
    [InlineData(false, true, -0.25)]
    [InlineData(true, false, -0.25)]
    [InlineData(true, true, -0.25)]
    [InlineData(false, false, double.NaN)]
    [InlineData(false, true, double.NaN)]
    [InlineData(true, false, double.NaN)]
    [InlineData(true, true, double.NaN)]
    [InlineData(false, false, double.NegativeInfinity)]
    [InlineData(false, true, double.NegativeInfinity)]
    [InlineData(true, false, double.NegativeInfinity)]
    [InlineData(true, true, double.NegativeInfinity)]
    public async Task NonMutatingRates_DoNotObserveTheConfiguredSource_EvenWhenItIsTheSharedNoChangeMutator(bool nsga, bool noChange, double rate)
    {
        IOperator<Mutation, int> source = noChange ? NoChangeMutator<int>.Instance : new StepMutator(1);
        var scope = ResolutionScope.Create();
        var algorithm = Algorithm(source) with { MutationRate = rate };
        IAlgorithm<int, PopulationState<int>> root = nsga ? AsNsga2(algorithm) : algorithm;
        await RunToCompletion(scope, root.Counting(source, out var counter, OperatorCountMetric.Candidates));
        scope.Results.Result(counter).CurrentCount.ShouldBe(0);
    }

    // Counts the selected mutator's candidates into a counter of its own and records the selection.
    private static IOperator<Mutation, int> Counting(IOperator<Mutation, int> selection, List<(IOperator<Mutation, int> Mutator, Counter Count)> selected)
    {
        var count = new Counter();
        selected.Add((selection, count));
        return selection.Counted(count, OperatorCountMetric.Candidates);
    }

    private static GeneticAlgorithm<int> Algorithm(IOperator<Mutation, int> mutator) => new()
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

    private static NSGA2<int> AsNsga2(GeneticAlgorithm<int> algorithm) => new()
    {
        Creator = algorithm.Creator,
        Crossover = algorithm.Crossover,
        Mutator = algorithm.Mutator,
        Selector = algorithm.Selector,
        Evaluator = algorithm.Evaluator,
        Refiner = algorithm.Refiner,
        Replacer = new ElitismReplacer<int>(elites: 0),
        PopulationSize = algorithm.PopulationSize,
        MaximumGenerations = algorithm.MaximumGenerations,
        MutationRate = algorithm.MutationRate
    };

    private static IAsyncEnumerable<PopulationState<int>> Run(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, Problem)
            .RunStreamingAsync(RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken);

    private static ValueTask<PopulationState<int>[]> States(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm) =>
        Run(scope, algorithm).ToArrayAsync(TestContext.Current.CancellationToken);

    private static async Task RunToCompletion(ResolutionScope scope, IAlgorithm<int, PopulationState<int>> algorithm)
    {
        var executor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, Problem);
        await executor.RunStreamingAsync(RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken)
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

    private sealed record RandomEvaluator : StatelessEvaluator<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            candidates.Select(candidate => new ObjectiveVector(candidate + random.NextDouble())).ToArray();
    }

    private sealed record RandomStepMutator : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) =>
            parents.Select(parent => parent - random.NextInt(1, 10)).ToArray();
    }
}
