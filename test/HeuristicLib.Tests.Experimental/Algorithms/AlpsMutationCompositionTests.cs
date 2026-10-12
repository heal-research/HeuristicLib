using HEAL.HeuristicLib.Operators.Creators;
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
    [InlineData(1.0)]
    public async Task InternalMutationRate_MatchesExplicitComposition_WithTheSameSeed(double rate)
    {
        var algorithm = CreateAlgorithm(rate);
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
        var scope = ResolutionScope.Create(builder => builder.Wrap(NodeSelector.OfType<IMutator<int>>().And(source =>
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
}
