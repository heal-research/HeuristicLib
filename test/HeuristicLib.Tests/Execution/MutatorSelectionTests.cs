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

    private static GeneticAlgorithm<int> Algorithm(IMutator<int> mutator) => new()
    {
        Creator = new ConstantCreator(),
        Crossover = SelectFirstParentCrossover<int>.Instance,
        Mutator = mutator,
        Selector = new RandomSelector<int>(),
        PopulationSize = 4,
        Elites = 0,
        MaximumGenerations = 3,
        MutationRate = 1
    };

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
