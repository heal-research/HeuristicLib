using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.LocalSearch;

public class HillClimberTests
{
    [Fact]
    public void Stream_WhenNoImprovingNeighborExists_YieldsOnlyGeneratedInitialState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = CreateHillClimber(initialValue: 0, mutationOffset: 1);

        var states = algorithm.TerminatedAfterIterations(5)
            .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Select(StateCandidate).ShouldBe([0]);
    }

    [Fact]
    public void Stream_WithInitialLocalOptimum_YieldsNoStates()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = CreateHillClimber(initialValue: 0, mutationOffset: 1);
        var initialState = SingleSolutionState.From(0, new ObjectiveVector(0.0));

        var states = algorithm.TerminatedAfterIterations(5)
            .Stream(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken)
            .ToList();

        states.ShouldBeEmpty();
    }

    private static HillClimber<int> CreateHillClimber(
        int initialValue,
        int mutationOffset)
    {
        return new HillClimber<int>
        {
            Creator = new ConstantCreator(initialValue),
            Mutator = new OffsetMutator(mutationOffset),
            Direction = LocalSearchDirection.FirstImprovement,
            MaxNeighbors = 4,
            BatchSize = 2
        };
    }

    private static int StateCandidate(SingleSolutionState<int> state) => state.EvaluatedCandidate.Candidate;

    private sealed record ConstantCreator(int Value)
        : IOperator<Creation, int>,
        ICreationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        IExecutor IOperator<Creation, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => this;

        public IReadOnlyList<int> Create(
            int count,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            IProblem<int, DummySearchSpace<int>> problem) =>
            Enumerable.Repeat(Value, count).ToArray();
    }

    private sealed record OffsetMutator(int Offset)
        : IOperator<Mutation, int>,
        IMutationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        IExecutor IOperator<Mutation, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => this;

        public IReadOnlyList<int> Mutate(
            IReadOnlyList<int> parents,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            IProblem<int, DummySearchSpace<int>> problem) =>
            parents.Select(parent => parent + Offset).ToArray();
    }
}
