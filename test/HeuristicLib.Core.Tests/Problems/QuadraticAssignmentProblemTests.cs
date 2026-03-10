using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests.Problems;

public class QuadraticAssignmentProblemTests
{
  [Fact]
  public void Constructor_ShouldExposeProblemData()
  {
    var data = new QuadraticAssignmentProblemData(
      new double[,] {
        { 0, 1 },
        { 2, 0 }
      },
      new double[,] {
        { 0, 3 },
        { 4, 0 }
      });

    var problem = new QuadraticAssignmentProblem(data);

    Assert.Equal(data, problem.ProblemData);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedCost_ForIdentityPermutation()
  {
    var data = new QuadraticAssignmentProblemData(
      new double[,] {
        { 0, 1 },
        { 2, 0 }
      },
      new double[,] {
        { 0, 10 },
        { 20, 0 }
      });

    var problem = new QuadraticAssignmentProblem(data);
    var solution = new Permutation(0, 1);
    var rng = DummyRandomNumberGenerator.Instance;

    var result = problem.Evaluate(solution, rng);

    // cost =
    // flow(0,0)*dist(0,0) + flow(0,1)*dist(0,1)
    // + flow(1,0)*dist(1,0) + flow(1,1)*dist(1,1)
    // = 0*0 + 1*10 + 2*20 + 0*0 = 50
    Assert.Equal((ObjectiveVector)50.0, result);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedCost_ForSwappedPermutation()
  {
    var data = new QuadraticAssignmentProblemData(
      new double[,] {
        { 0, 1 },
        { 2, 0 }
      },
      new double[,] {
        { 0, 10 },
        { 20, 0 }
      });

    var problem = new QuadraticAssignmentProblem(data);
    var solution = new Permutation(1, 0);
    var rng = DummyRandomNumberGenerator.Instance;

    var result = problem.Evaluate(solution, rng);

    // facility 0 -> location 1
    // facility 1 -> location 0
    //
    // cost =
    // flow(0,0)*dist(1,1) + flow(0,1)*dist(1,0)
    // + flow(1,0)*dist(0,1) + flow(1,1)*dist(0,0)
    // = 0*0 + 1*20 + 2*10 + 0*0 = 40
    Assert.Equal((ObjectiveVector)40.0, result);
  }
}
