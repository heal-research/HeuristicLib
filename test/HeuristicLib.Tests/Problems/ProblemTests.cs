using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Problems;

public class FuncProblemTests
{
    [Fact]
    public void Create_ShouldSetObjectiveAndSearchSpace()
    {
        var searchSpace = DummySearchSpace<int>.Instance;
        var objective = SingleObjective.Minimize;

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: searchSpace,
          objective: objective);

        problem.Objective.ShouldBe(objective);
        problem.SearchSpace.ShouldBe(searchSpace);
    }

    [Fact]
    public void Create_ShouldReturnProblem_ThatUsesProvidedEvaluateFunction()
    {
        // Arrange
        var searchSpace = DummySearchSpace<int>.Instance;
        var objective = SingleObjective.Minimize;
        var rng = DummyRandomNumberGenerator.Instance;

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x * 2.5,
          encoding: searchSpace,
          objective: objective);

        // Act
        var result = problem.Evaluate(4, rng);

        // Assert
        result.ShouldBe((ObjectiveVector)10.0);
    }

    [Fact]
    public void Evaluate_ShouldCallEvaluateFunction_WithGivenSolution()
    {
        // Arrange
        var searchSpace = DummySearchSpace<int>.Instance;
        var objective = SingleObjective.Minimize;
        var rng = DummyRandomNumberGenerator.Instance;

        int? receivedSolution = null;

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) =>
          {
              receivedSolution = x;
              return 123.0;
          },
          encoding: searchSpace,
          objective: objective);

        // Act
        var result = problem.Evaluate(42, rng);

        // Assert
        receivedSolution.ShouldBe(42);
        result.ShouldBe((ObjectiveVector)123.0);
    }

    [Fact]
    public void Evaluate_ShouldCallFunctionEveryTime()
    {
        var searchSpace = DummySearchSpace<int>.Instance;
        var objective = SingleObjective.Minimize;
        var rng = DummyRandomNumberGenerator.Instance;

        int calls = 0;

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) =>
          {
              calls++;
              return x;
          },
          encoding: searchSpace,
          objective: objective);

        problem.Evaluate(1, rng);
        problem.Evaluate(2, rng);

        calls.ShouldBe(2);
    }
}
