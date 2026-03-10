using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests.Problems;

public class FuncProblemTests
{
  [Fact]
  public void Create_ShouldSetObjectiveAndSearchSpace()
  {
    var searchSpace = DummySearchSpace<int>.Instance;
    var objective = SingleObjective.Minimize;

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: searchSpace,
      objective: objective);

    Assert.Equal(objective, problem.Objective);
    Assert.Equal(searchSpace, problem.SearchSpace);
  }

  [Fact]
  public void Create_ShouldReturnProblem_ThatUsesProvidedEvaluateFunction()
  {
    // Arrange
    var searchSpace = DummySearchSpace<int>.Instance;
    var objective = SingleObjective.Minimize;
    var rng = DummyRandomNumberGenerator.Instance;

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x * 2.5,
      encoding: searchSpace,
      objective: objective);

    // Act
    var result = problem.Evaluate(4, rng);

    // Assert
    Assert.Equal((ObjectiveVector)10.0, result);
  }

  [Fact]
  public void Evaluate_ShouldCallEvaluateFunction_WithGivenSolution()
  {
    // Arrange
    var searchSpace = DummySearchSpace<int>.Instance;
    var objective = SingleObjective.Minimize;
    var rng = DummyRandomNumberGenerator.Instance;

    int? receivedSolution = null;

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => {
        receivedSolution = x;
        return 123.0;
      },
      encoding: searchSpace,
      objective: objective);

    // Act
    var result = problem.Evaluate(42, rng);

    // Assert
    Assert.Equal(42, receivedSolution);
    Assert.Equal((ObjectiveVector)123.0, result);
  }

  [Fact]
  public void Evaluate_ShouldCallFunctionEveryTime()
  {
    var searchSpace = DummySearchSpace<int>.Instance;
    var objective = SingleObjective.Minimize;
    var rng = DummyRandomNumberGenerator.Instance;

    int calls = 0;

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => {
        calls++;
        return x;
      },
      encoding: searchSpace,
      objective: objective);

    problem.Evaluate(1, rng);
    problem.Evaluate(2, rng);

    Assert.Equal(2, calls);
  }
}
