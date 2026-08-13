using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.LocalSearch;

public class HillClimberTests
{
    [Fact]
    public void Stream_WhenNoImprovingNeighborExists_YieldsOnlyGeneratedInitialState()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = CreateHillClimber(initialValue: 0, mutationOffset: 1);

        var states = algorithm.WithMaxIterations(5)
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

        var states = algorithm.WithMaxIterations(5)
          .Stream(problem, RandomNumberGenerator.Create(42), initialState, TestContext.Current.CancellationToken)
          .ToList();

        states.ShouldBeEmpty();
    }

    private static HillClimber<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateHillClimber(
      int initialValue,
      int mutationOffset)
    {
        return new HillClimber<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
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
      : ICreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>,
        ICreatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public ICreatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

        public IReadOnlyList<int> Create(
          int count,
          IRandomNumberGenerator random,
          DummySearchSpace<int> searchSpace,
          IProblem<int, DummySearchSpace<int>> problem) =>
          Enumerable.Repeat(Value, count).ToArray();
    }

    private sealed record OffsetMutator(int Offset)
      : IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>,
        IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

        public IReadOnlyList<int> Mutate(
          IReadOnlyList<int> parents,
          IRandomNumberGenerator random,
          DummySearchSpace<int> searchSpace,
          IProblem<int, DummySearchSpace<int>> problem) =>
          parents.Select(parent => parent + Offset).ToArray();
    }
}
