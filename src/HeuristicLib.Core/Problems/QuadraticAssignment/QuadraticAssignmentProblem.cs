using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public class QuadraticAssignmentProblem(IQuadraticAssignmentProblemData problemData)
  : PermutationProblem(SingleObjective.Minimize, new PermutationSearchSpace(problemData.Size))
{
  public IQuadraticAssignmentProblemData ProblemData { get; } = problemData;

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random)
  {
    // solution[facility] = location
    var n = ProblemData.Size;
    var cost = 0.0;
    for (var i = 0; i < n; i++) {
      var li = solution[i];
      for (var j = 0; j < n; j++) {
        var lj = solution[j];
        cost += ProblemData.GetFlow(i, j) * ProblemData.GetDistance(li, lj);
      }
    }

    return cost;
  }

  #region Default Instance

  public static QuadraticAssignmentProblem CreateDefault()
  {
    var flows = new double[,] { { 0, 2, 0, 1 }, { 2, 0, 3, 0 }, { 0, 3, 0, 4 }, { 1, 0, 4, 0 } };
    var distances = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };

    return new QuadraticAssignmentProblem(new QuadraticAssignmentProblemData(flows, distances));
  }

  #endregion

}
