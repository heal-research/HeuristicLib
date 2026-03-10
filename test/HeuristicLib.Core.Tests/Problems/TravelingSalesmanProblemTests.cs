using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests.Problems;

public class TravelingSalesmanProblemTests
{
  [Fact]
  public void Constructor_ShouldExposeProblemData()
  {
    var data = new TravelingSalesmanDistanceMatrixProblemData(new double[,] {
      { 0, 1 },
      { 1, 0 }
    });

    var problem = new TravelingSalesmanProblem(data);

    Assert.Equal(data, problem.ProblemData);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedTourLength()
  {
    var data = new TravelingSalesmanDistanceMatrixProblemData(new double[,] {
      { 0, 2, 9 },
      { 1, 0, 6 },
      { 15, 7, 0 }
    });

    var problem = new TravelingSalesmanProblem(data);
    var solution = new Permutation(0, 1, 2);
    var rng = DummyRandomNumberGenerator.Instance;

    var result = problem.Evaluate(solution, rng);

    // 0 -> 1 = 2
    // 1 -> 2 = 6
    // 2 -> 0 = 15
    Assert.Equal((ObjectiveVector)23.0, result);
  }

  [Fact]
  public void Evaluate_ShouldIncludeReturnToStartingCity()
  {
    var data = new TravelingSalesmanDistanceMatrixProblemData(new double[,] {
      { 0, 5, 1 },
      { 5, 0, 2 },
      { 10, 2, 0 }
    });

    var problem = new TravelingSalesmanProblem(data);
    var solution = new Permutation(0, 2, 1);
    var rng = DummyRandomNumberGenerator.Instance;

    var result = problem.Evaluate(solution, rng);

    // 0 -> 2 = 1
    // 2 -> 1 = 2
    // 1 -> 0 = 5
    Assert.Equal((ObjectiveVector)8.0, result);
  }

  [Fact]
  public void DefaultConstructor_ShouldCreateUsableProblem()
  {
    var problem = new TravelingSalesmanProblem();
    var rng = DummyRandomNumberGenerator.Instance;
    var solution = new Permutation(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

    var result = problem.Evaluate(solution, rng);

    Assert.True(result[0] > 0);
  }

  [Fact]
  public void CreateDefault_ShouldCreateUsableProblem()
  {
    var problem = TravelingSalesmanProblem.CreateDefault();
    var rng = DummyRandomNumberGenerator.Instance;
    var solution = new Permutation(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);

    var result = problem.Evaluate(solution, rng);

    Assert.True(result[0] > 0);
  }
}
