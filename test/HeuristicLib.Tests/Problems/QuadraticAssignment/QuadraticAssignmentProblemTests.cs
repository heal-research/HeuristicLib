using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Problems.QuadraticAssignment;

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

        problem.ProblemData.ShouldBe(data);
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
        Permutation solution = [0, 1];
        var rng = RandomNumberGenerator.Create(0);

        var result = problem.Evaluate(solution, rng);

        // cost =
        // flow(0,0)*dist(0,0) + flow(0,1)*dist(0,1)
        // + flow(1,0)*dist(1,0) + flow(1,1)*dist(1,1)
        // = 0*0 + 1*10 + 2*20 + 0*0 = 50
        result.ShouldBe((ObjectiveVector)50.0);
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
        Permutation solution = [1, 0];
        var rng = RandomNumberGenerator.Create(0);

        var result = problem.Evaluate(solution, rng);

        // facility 0 -> location 1
        // facility 1 -> location 0
        //
        // cost =
        // flow(0,0)*dist(1,1) + flow(0,1)*dist(1,0)
        // + flow(1,0)*dist(0,1) + flow(1,1)*dist(0,0)
        // = 0*0 + 1*20 + 2*10 + 0*0 = 40
        result.ShouldBe((ObjectiveVector)40.0);
    }
}
