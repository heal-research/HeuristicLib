using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class AlpsMutationCompositionTests
{
    private static readonly IProblem<int, UnrestrictedSearchSpace<int>> Problem =
        FuncProblem.Create(static (int candidate) => candidate, UnrestrictedSearchSpace<int>.Instance, SingleObjective.Minimize);

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.35)]
    [InlineData(0.5)]
    [InlineData(-0.25)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(double.PositiveInfinity)]
    public async Task MutationRate_MatchesExplicitAppliedAtRate_WithTheSameSeed(double rate)
    {
        var algorithm = CreateAlgorithm(rate) with { Mutator = new RandomStepMutator(), Evaluator = new RandomEvaluator() };
        var baseline = algorithm with
        {
            Mutator = rate >= 1 ? algorithm.Mutator : algorithm.Mutator.AppliedAtRate(rate),
            MutationRate = 1
        };
        var actual = await States(ResolutionScope.Create(), algorithm);
        var expected = await States(ResolutionScope.Create(), baseline);
        actual.Length.ShouldBe(expected.Length);
        for (var i = 0; i < actual.Length; i++)
        {
            actual[i].Population.Single().EvaluatedCandidates.ShouldBe(expected[i].Population.Single().EvaluatedCandidates);
            actual[i].Ages.Single().ShouldBe(expected[i].Ages.Single());
        }
    }

    [Fact]
    public async Task BroadMutatorSelection_SeesOnlyTheConfiguredSource_AndCountsOnlyRoutedOffspring()
    {
        var algorithm = CreateAlgorithm(0.5) with { MaximumGenerations = 2 };
        var offered = new List<IMutator<int>>();
        var count = new CountAccumulator();
        var scope = ResolutionScope.Create(builder => builder.Install(Pointcut.OfType<IMutator<int>>().And(source =>
        {
            offered.Add(source);
            return true;
        }), source => source.CountCandidates(count)));

        var states = await States(scope, algorithm);
        offered.ShouldBe([algorithm.Mutator]);
        var mutated = states[1].Population.Single().EvaluatedCandidates.Count(candidate => candidate.Candidate == 99);
        mutated.ShouldBeInRange(1, 15);
        count.CurrentCount.ShouldBe(mutated);
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(true, 0.0)]
    [InlineData(false, -0.25)]
    [InlineData(true, -0.25)]
    [InlineData(false, double.NaN)]
    [InlineData(true, double.NaN)]
    [InlineData(false, double.NegativeInfinity)]
    [InlineData(true, double.NegativeInfinity)]
    public async Task NonMutatingRates_DoNotObserveTheConfiguredSource_EvenWhenItIsTheSharedNoChangeMutator(bool noChange, double rate)
    {
        var algorithm = CreateAlgorithm(rate);
        if (noChange)
            algorithm = algorithm with { Mutator = NoChangeMutator<int>.Instance };
        var count = new CountAccumulator();
        var scope = ResolutionScope.Create(builder => builder.Install(algorithm.Mutator, source => source.CountCandidates(count)));

        await States(scope, algorithm);

        count.CurrentCount.ShouldBe(0);
    }

    private static AlpsGeneticAlgorithm<int> CreateAlgorithm(double rate) => new()
    {
        Creator = new ConstantCreator(),
        Crossover = SelectFirstParentCrossover<int>.Instance,
        Mutator = new StepMutator(),
        Selector = new RandomSelector<int>(),
        PopulationSize = 16,
        MaximumGenerations = 5,
        Elites = 0,
        MutationRate = rate
    };

    private static ValueTask<AlpsState<int>[]> States(ResolutionScope scope, AlpsGeneticAlgorithm<int> algorithm) =>
        scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, AlpsState<int>>(algorithm)
            .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);

    private sealed record ConstantCreator : SingleCandidateCreator<int>
    {
        public override int CreateCandidate(IRandomNumberGenerator random) => 100;
    }

    private sealed record StepMutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent - 1;
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
