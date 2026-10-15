using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Terminators;

public class StagnationTerminatorTests
{
    [Fact]
    public void TerminatesWhenConsecutiveNonImprovingStatesReachThreshold()
    {
        var problem = CreateProblem();
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(StagnationTerminator.For(problem, stagnationThreshold: 2));

        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void StrictImprovementResetsConsecutiveNonImprovingStateCount()
    {
        var problem = CreateProblem();
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(StagnationTerminator.For(problem, stagnationThreshold: 2));

        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(2), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(2), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(CreateState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveThresholdTerminatesOnFirstCheckedProducedState(int threshold)
    {
        var problem = CreateProblem();
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(StagnationTerminator.For(problem, threshold));

        execution.IsTerminalState(CreateState(3), problem.SearchSpace, problem).ShouldBeTrue();
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private static PopulationState<int> CreateState(double objective) =>
        Population.From([EvaluatedCandidate.From(0, new ObjectiveVector(objective))]).ToPopulationState();
}
